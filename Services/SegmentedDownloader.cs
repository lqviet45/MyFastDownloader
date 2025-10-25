using System.Buffers;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using MyFastDownloader.App.Models;

namespace MyFastDownloader.App.Services;

public class SegmentedDownloader
{
    private readonly HttpClient _http;
    private readonly int _maxParallel;
    private readonly int _bufferSize = 1 << 15; // 32KB
    private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
    private DateTime _lastProgressUpdate = DateTime.MinValue;
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(100);

    public event Action<long, long>? Progress; // (downloaded, total)
    public event Action<double>? Speed; // bytes/sec

    public SegmentedDownloader(int maxParallel = 4, HttpMessageHandler? handler = null)
    {
        _maxParallel = Math.Max(1, maxParallel);
        _http = handler is null
            ? new HttpClient(new SocketsHttpHandler
                {
                    AllowAutoRedirect = true,
                    AutomaticDecompression = System.Net.DecompressionMethods.All,
                    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                    MaxConnectionsPerServer = 10,
                })
                { Timeout = TimeSpan.FromMinutes(10) }
            : new HttpClient(handler);
    }
    
    public async Task<long> GetContentLengthAsync(string url, CancellationToken token)
    {
        try
        {
            using var head = new HttpRequestMessage(HttpMethod.Head, url);
            using var res = await _http.SendAsync(head, token);
            if (res.IsSuccessStatusCode && res.Content.Headers.ContentLength.HasValue)
                return res.Content.Headers.ContentLength.Value;
        }
        catch { /* Ignore */ }

        try
        {
            using var req2 = new HttpRequestMessage(HttpMethod.Get, url);
            req2.Headers.Range = new RangeHeaderValue(0, 0);
            using var res2 = await _http.SendAsync(req2, HttpCompletionOption.ResponseHeadersRead, token);
            var cr = res2.Content.Headers.ContentRange;
            if (cr?.Length is long len) return len;
        }
        catch { /* Ignore */ }

        return -1;
    }
    
    public async Task StartAsync(string url, string filePath, int segmentsCount, CancellationToken token)
    {
        var metaPath = filePath + ".meta.json";
        DownloadMetadata meta;

        if (File.Exists(metaPath))
        {
            var json = await File.ReadAllTextAsync(metaPath, token);
            meta = JsonSerializer.Deserialize<DownloadMetadata>(json)!;
        }
        else
        {
            var total = await GetContentLengthAsync(url, token);
            if (total <= 0) 
                throw new InvalidOperationException("Server không hỗ trợ tải xuống (không có Content-Length)");
            
            // Calculate optimal segments based on file size
            segmentsCount = CalculateOptimalSegments(total, segmentsCount);
            
            meta = new DownloadMetadata { Url = url, FilePath = filePath, TotalSize = total };
            long part = total / segmentsCount;
            long cur = 0;
            for (int i = 0; i < segmentsCount; i++)
            {
                long from = cur;
                long to = (i == segmentsCount - 1) ? total - 1 : cur + part - 1;
                meta.Segments.Add(new DownloadSegment { From = from, To = to });
                cur += part;
            }
            await File.WriteAllTextAsync(metaPath, JsonSerializer.Serialize(meta), token);
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            fs.SetLength(total);
        }

        var semaphore = new SemaphoreSlim(_maxParallel);
        var tasks = new List<Task>();
        long prevBytes = 0;

        // Ticker for progress and speed updates (throttled)
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;
                    if (now - _lastProgressUpdate >= ProgressInterval)
                    {
                        _lastProgressUpdate = now;
                        var downloaded = meta.Segments.Sum(s => s.Downloaded);
                        Progress?.Invoke(downloaded, meta.TotalSize);
                        var delta = downloaded - prevBytes;
                        prevBytes = downloaded;
                        Speed?.Invoke(delta);
                    }
                    await Task.Delay(100, token);
                }
                catch { break; }
            }
        }, token);

        foreach (var seg in meta.Segments.Where(s => !s.Completed))
        {
            await semaphore.WaitAsync(token);
            tasks.Add(Task.Run(async () =>
            {
                try { await DownloadSegmentAsync(meta, seg, metaPath, token); }
                finally { semaphore.Release(); }
            }, token));
        }

        await Task.WhenAll(tasks);
        bool allDone = meta.Segments.All(s => s.Completed);
        
        // Final progress update
        var finalDownloaded = meta.Segments.Sum(s => s.Downloaded);
        Progress?.Invoke(finalDownloaded, meta.TotalSize);
        
        if (allDone && File.Exists(metaPath)) 
            File.Delete(metaPath);
    }

    private int CalculateOptimalSegments(long fileSize, int requestedSegments)
    {
        // Adaptive segment count based on file size
        var optimal = fileSize switch
        {
            < 1024 * 1024 => 1,              // <1MB: 1 segment
            < 5 * 1024 * 1024 => 2,          // 1-5MB: 2 segments
            < 10 * 1024 * 1024 => 4,         // 5-10MB: 4 segments
            < 50 * 1024 * 1024 => 6,         // 10-50MB: 6 segments
            < 100 * 1024 * 1024 => 8,        // 50-100MB: 8 segments
            _ => 10                           // >100MB: 10 segments
        };
        
        return Math.Min(optimal, requestedSegments);
    }

    private async Task DownloadSegmentAsync(DownloadMetadata meta, DownloadSegment seg, string metaPath,
        CancellationToken token)
    {
        int maxRetries = 5, attempt = 0;
        byte[]? buffer = null;
        
        try
        {
            buffer = _bufferPool.Rent(_bufferSize);
            
            while (!seg.Completed && attempt < maxRetries && !token.IsCancellationRequested)
            {
                attempt++;
                try
                {
                    long start = seg.From + seg.Downloaded;
                    long end = seg.To;

                    using var req = new HttpRequestMessage(HttpMethod.Get, meta.Url);
                    req.Headers.Range = new RangeHeaderValue(start, end);
                    using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, token);
                    res.EnsureSuccessStatusCode();

                    await using var stream = await res.Content.ReadAsStreamAsync(token);
                    await using var fs = new FileStream(meta.FilePath, FileMode.Open, FileAccess.Write, FileShare.Read);
                    fs.Seek(start, SeekOrigin.Begin);

                    int read;
                    while ((read = await stream.ReadAsync(buffer.AsMemory(0, _bufferSize), token)) > 0)
                    {
                        await fs.WriteAsync(buffer.AsMemory(0, read), token);
                        seg.Downloaded += read;

                        // Save metadata periodically
                        if (seg.Downloaded % (256 * 1024) < _bufferSize)
                        {
                            var json = JsonSerializer.Serialize(meta);
                            await File.WriteAllTextAsync(metaPath, json, token);
                        }
                    }
                    break; // Success
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    if (attempt < maxRetries)
                        await Task.Delay(1000 * attempt, CancellationToken.None);
                }
            }
        }
        finally
        {
            if (buffer != null)
                _bufferPool.Return(buffer);
        }
    }
}