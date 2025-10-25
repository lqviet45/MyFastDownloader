using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MyFastDownloader.App.Models;

namespace MyFastDownloader.App.Services;

public class SegmentedDownloader
{
    private readonly HttpClient _http;
    private readonly int _maxParallel;
    private readonly int _bufferSize = 1 << 16; // 64KB (increased from 32KB)
    private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
    private DateTime _lastProgressUpdate = DateTime.MinValue;
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(100);

    public event Action<long, long>? Progress;
    public event Action<double>? Speed;

    public SegmentedDownloader(int maxParallel = 16, HttpMessageHandler? handler = null)
    {
        _maxParallel = Math.Max(1, maxParallel);
        _http = handler is null
            ? new HttpClient(new SocketsHttpHandler
                {
                    AllowAutoRedirect = true,
                    AutomaticDecompression = System.Net.DecompressionMethods.All,
                    PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(10),
                    MaxConnectionsPerServer = 100, // Increased from 10
                    EnableMultipleHttp2Connections = true,
                })
                { 
                    Timeout = TimeSpan.FromMinutes(30),
                    DefaultRequestVersion = new Version(2, 0) // Try HTTP/2 first
                }
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
        catch { }

        try
        {
            using var req2 = new HttpRequestMessage(HttpMethod.Get, url);
            req2.Headers.Range = new RangeHeaderValue(0, 0);
            using var res2 = await _http.SendAsync(req2, HttpCompletionOption.ResponseHeadersRead, token);
            var cr = res2.Content.Headers.ContentRange;
            if (cr?.Length is long len) return len;
        }
        catch { }

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
            
            // AGGRESSIVE segment calculation for maximum speed
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
            
            // Pre-allocate file (faster than growing)
            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 
                                         bufferSize: 1 << 16, FileOptions.WriteThrough);
            fs.SetLength(total);
        }

        var semaphore = new SemaphoreSlim(_maxParallel);
        var tasks = new System.Collections.Generic.List<Task>();
        long prevBytes = 0;
        var speedSamples = new System.Collections.Generic.Queue<(DateTime time, long bytes)>();

        // High-frequency speed calculation
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
                        
                        // Calculate speed with moving average (last 5 samples)
                        speedSamples.Enqueue((now, downloaded));
                        while (speedSamples.Count > 5)
                            speedSamples.Dequeue();
                        
                        if (speedSamples.Count >= 2)
                        {
                            var first = speedSamples.First();
                            var last = speedSamples.Last();
                            var timeDiff = (last.time - first.time).TotalSeconds;
                            if (timeDiff > 0)
                            {
                                var bytesDiff = last.bytes - first.bytes;
                                var speed = bytesDiff / timeDiff;
                                Speed?.Invoke(speed);
                            }
                        }
                    }
                    await Task.Delay(100, token);
                }
                catch { break; }
            }
        }, token);

        // Download all segments in parallel
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
        // AGGRESSIVE segmentation for maximum speed
        var optimal = fileSize switch
        {
            < 1024 * 1024 => 2,              // <1MB: 2 segments
            < 5 * 1024 * 1024 => 4,          // 1-5MB: 4 segments
            < 10 * 1024 * 1024 => 8,         // 5-10MB: 8 segments
            < 50 * 1024 * 1024 => 16,        // 10-50MB: 16 segments
            < 100 * 1024 * 1024 => 24,       // 50-100MB: 24 segments
            < 500 * 1024 * 1024 => 32,       // 100-500MB: 32 segments
            _ => 48                           // >500MB: 48 segments
        };
        
        return Math.Max(optimal, requestedSegments);
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
                    req.Version = new Version(2, 0); // Prefer HTTP/2
                    
                    using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, token);
                    res.EnsureSuccessStatusCode();

                    await using var stream = await res.Content.ReadAsStreamAsync(token);
                    
                    // Use async file I/O with larger buffer and no buffering
                    await using var fs = new FileStream(meta.FilePath, FileMode.Open, FileAccess.Write, 
                                                       FileShare.Read, _bufferSize, 
                                                       FileOptions.Asynchronous | FileOptions.WriteThrough);
                    fs.Seek(start, SeekOrigin.Begin);

                    int read;
                    long totalRead = 0;
                    while ((read = await stream.ReadAsync(buffer.AsMemory(0, _bufferSize), token)) > 0)
                    {
                        await fs.WriteAsync(buffer.AsMemory(0, read), token);
                        seg.Downloaded += read;
                        totalRead += read;

                        // Save metadata less frequently for speed (every 1MB instead of 256KB)
                        if (totalRead % (1024 * 1024) < _bufferSize)
                        {
                            var json = JsonSerializer.Serialize(meta);
                            await File.WriteAllTextAsync(metaPath, json, token);
                            totalRead = 0; // Reset counter
                        }
                    }
                    
                    // Final save
                    if (seg.Completed)
                    {
                        var json = JsonSerializer.Serialize(meta);
                        await File.WriteAllTextAsync(metaPath, json, token);
                    }
                    
                    break; // Success
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (attempt < maxRetries)
                    {
                        // Exponential backoff with jitter
                        var delay = (int)(1000 * Math.Pow(1.5, attempt) + Random.Shared.Next(100, 500));
                        await Task.Delay(delay, CancellationToken.None);
                    }
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