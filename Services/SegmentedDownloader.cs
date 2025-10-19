using System;
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
    private readonly int _bufferSize = 1 << 15; // 32KB

    public event Action<long, long>? Progress; // (downloaded, total)
    public event Action<double>? Speed; // bytes/sec (tick 1s)

    public SegmentedDownloader(int maxParallel = 4, HttpMessageHandler? handler = null)
    {
        _maxParallel = Math.Max(1, maxParallel);
        _http = handler is null
            ? new HttpClient(new SocketsHttpHandler
                {
                    AllowAutoRedirect = true,
                    AutomaticDecompression = System.Net.DecompressionMethods.All
                })
                { Timeout = TimeSpan.FromMinutes(10) }
            : new HttpClient(handler);
    }
    
    public async Task<long> GetContentLengthAsync(string url, CancellationToken token)
    {
        using var head = new HttpRequestMessage(HttpMethod.Head, url);
        using var res = await _http.SendAsync(head, token);
        if (res.IsSuccessStatusCode && res.Content.Headers.ContentLength.HasValue)
            return res.Content.Headers.ContentLength.Value;

        using var req2 = new HttpRequestMessage(HttpMethod.Get, url);
        req2.Headers.Range = new RangeHeaderValue(0, 0);
        using var res2 = await _http.SendAsync(req2, HttpCompletionOption.ResponseHeadersRead, token);
        var cr = res2.Content.Headers.ContentRange;
        if (cr?.Length is long len) return len;

        return -1;
    }
    
    public async Task StartAsync(string url, string filePath, int segmentsCount, CancellationToken token)
    {
        var metaPath = filePath + ".meta.json";
        DownloadMetadata meta;

        if (File.Exists(metaPath))
        {
            meta = JsonSerializer.Deserialize<DownloadMetadata>(await File.ReadAllTextAsync(metaPath, token))!;
        }
        else
        {
            var total = await GetContentLengthAsync(url, token);
            if (total <= 0) throw new InvalidOperationException("Server không trả Content-Length hoặc không hỗ trợ Range.");
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
        var tasks = new System.Collections.Generic.List<Task>();
        long prevBytes = 0;

        // ticker 1s để tính speed + raise progress
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var downloaded = meta.Segments.Sum(s => s.Downloaded);
                    Progress?.Invoke(downloaded, meta.TotalSize);
                    var delta = downloaded - prevBytes;
                    prevBytes = downloaded;
                    Speed?.Invoke(delta); // bytes per second tick
                    await Task.Delay(1000, token);
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
        if (allDone && File.Exists(metaPath)) File.Delete(metaPath);
    }

    private async Task DownloadSegmentAsync(DownloadMetadata meta, DownloadSegment seg, string metaPath,
        CancellationToken token)
    {
        int maxRetries = 5, attempt = 0;
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

                var buffer = new byte[_bufferSize];
                int read;
                while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token)) > 0)
                {
                    await fs.WriteAsync(buffer.AsMemory(0, read), token);
                    seg.Downloaded += read;

                    // lưu meta định kỳ ~256KB
                    if (seg.Downloaded % (256 * 1024) < _bufferSize)
                        await File.WriteAllTextAsync(metaPath, JsonSerializer.Serialize(meta), token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                await Task.Delay(1000 * attempt, CancellationToken.None);
            }
        }
    }
}