using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MyFastDownloader.App.Models;

namespace MyFastDownloader.App.Services;

public class SegmentedDownloader : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;
    private readonly int _maxParallel;
    private readonly int _bufferSize = 1 << 15; // 32KB
    private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;
    private DateTime _lastProgressUpdate = DateTime.MinValue;
    private static readonly TimeSpan ProgressInterval = TimeSpan.FromMilliseconds(100);
    private readonly object _progressLock = new object();
    private bool _disposed = false;

    // Speed calculation with moving average
    private readonly Queue<SpeedSample> _speedSamples = new();
    private const int MaxSpeedSamples = 10; // Keep last 10 samples (5 seconds at 500ms intervals)
    private DateTime _lastSpeedSample = DateTime.MinValue;
    private long _lastSpeedBytes = 0;

    public event Action<long, long>? Progress;
    public event Action<double>? Speed;

    private class SpeedSample
    {
        public DateTime Time { get; set; }
        public double BytesPerSecond { get; set; }
    }

    public SegmentedDownloader(int maxParallel = 8, HttpMessageHandler? handler = null)
    {
        _maxParallel = Math.Max(1, Math.Min(maxParallel, 16));
        
        if (handler != null)
        {
            _http = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(30) };
            _ownsHttpClient = true;
        }
        else
        {
            _http = HttpClientManager.Instance;
            _ownsHttpClient = false;
        }
        
        LogDebug($"Initialized with {_maxParallel} parallel connections");
    }
    
    private void LogDebug(string message)
    {
        var msg = $"[SegmentedDownloader] {DateTime.Now:HH:mm:ss.fff} - {message}";
        Debug.WriteLine(msg);
        Console.WriteLine(msg);
    }
    
    private void LogError(string message, Exception? ex = null)
    {
        var msg = $"[ERROR] {DateTime.Now:HH:mm:ss.fff} - {message}";
        Debug.WriteLine(msg);
        Console.WriteLine(msg);
        if (ex != null)
        {
            var exMsg = $"[ERROR] Exception: {ex.GetType().Name}: {ex.Message}";
            Debug.WriteLine(exMsg);
            Console.WriteLine(exMsg);
            
            if (ex is not OperationCanceledException)
            {
                Debug.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
            }
        }
    }
    
    public async Task<long> GetContentLengthAsync(string url, CancellationToken token)
    {
        LogDebug($"Getting content length for: {url}");
        
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            request.Headers.ConnectionClose = false;
            
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            
            if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength.HasValue)
            {
                var contentLength = response.Content.Headers.ContentLength.Value;
                LogDebug($"Content-Length from HEAD: {contentLength:N0} bytes");
                
                if (response.Headers.AcceptRanges?.Contains("bytes") == true)
                {
                    LogDebug("Server supports range requests (Accept-Ranges: bytes)");
                }
                
                return contentLength;
            }
        }
        catch (HttpRequestException ex)
        {
            LogError($"HEAD request failed (HttpRequestException): {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            LogError($"HEAD request timed out: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            LogError($"HEAD request failed: {ex.Message}", ex);
        }

        try
        {
            LogDebug("Trying range request fallback...");
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new RangeHeaderValue(0, 0);
            request.Headers.ConnectionClose = false;
            
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            
            if (response.StatusCode == HttpStatusCode.PartialContent || response.IsSuccessStatusCode)
            {
                var cr = response.Content.Headers.ContentRange;
                if (cr?.Length is long rangeLength)
                {
                    LogDebug($"Content-Length from Range request: {rangeLength:N0} bytes");
                    return rangeLength;
                }
                
                if (response.Content.Headers.ContentLength.HasValue)
                {
                    var contentLength = response.Content.Headers.ContentLength.Value;
                    LogDebug($"Content-Length from header: {contentLength:N0} bytes");
                    return contentLength;
                }
            }
        }
        catch (TaskCanceledException ex)
        {
            LogError($"Range request timed out: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            LogError($"Range request failed: {ex.Message}", ex);
        }

        LogError("Could not determine content length");
        return -1;
    }
    
    public async Task StartAsync(string url, string filePath, int segmentsCount, CancellationToken token)
    {
        LogDebug($"Starting download from: {url}");
        LogDebug($"Saving to: {filePath}");
        
        var metaPath = filePath + ".meta.json";
        DownloadMetadata meta;

        if (File.Exists(metaPath))
        {
            LogDebug("Resuming download...");
            try
            {
                var json = await File.ReadAllTextAsync(metaPath, token);
                meta = JsonSerializer.Deserialize<DownloadMetadata>(json)!;
                
                var completedSegments = meta.Segments.Count(s => s.Completed);
                var totalDownloaded = meta.Segments.Sum(s => s.Downloaded);
                LogDebug($"Progress: {completedSegments}/{meta.Segments.Count} segments, {totalDownloaded:N0}/{meta.TotalSize:N0} bytes");
            }
            catch (Exception ex)
            {
                LogError($"Failed to load metadata: {ex.Message}");
                File.Delete(metaPath);
                meta = await CreateNewDownload(url, filePath, segmentsCount, metaPath, token);
            }
        }
        else
        {
            meta = await CreateNewDownload(url, filePath, segmentsCount, metaPath, token);
        }

        // Initialize speed tracking
        _lastSpeedBytes = meta.Segments.Sum(s => s.Downloaded);
        _lastSpeedSample = DateTime.Now;
        _speedSamples.Clear();

        // Start progress monitoring
        using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        var progressTask = MonitorProgressAsync(meta, progressCts.Token);

        var incompletedSegments = meta.Segments.Where(s => !s.Completed).ToList();
        LogDebug($"Downloading {incompletedSegments.Count} segments with {_maxParallel} parallel connections");
        
        var semaphore = new SemaphoreSlim(_maxParallel);
        var tasks = new List<Task>();
        int segmentIndex = 0;

        foreach (var seg in incompletedSegments)
        {
            var index = segmentIndex++;
            await semaphore.WaitAsync(token);
            
            tasks.Add(Task.Run(async () =>
            {
                try 
                { 
                    await DownloadSegmentAsync(meta, seg, index, metaPath, token);
                }
                catch (OperationCanceledException)
                {
                    LogDebug($"Segment {index} cancelled");
                }
                catch (Exception ex)
                {
                    LogError($"Segment {index} failed", ex);
                }
                finally 
                { 
                    semaphore.Release(); 
                }
            }, token));
            
            if (segmentIndex < incompletedSegments.Count)
                await Task.Delay(100, token);
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch { }
        finally
        {
            progressCts.Cancel();
            try { await progressTask; } catch { }
        }
        
        bool allDone = meta.Segments.All(s => s.Completed);
        var finalDownloaded = meta.Segments.Sum(s => s.Downloaded);
        Progress?.Invoke(finalDownloaded, meta.TotalSize);
        
        if (allDone)
        {
            LogDebug($"Download completed! {finalDownloaded:N0} bytes");
            if (File.Exists(metaPath))
            {
                try { File.Delete(metaPath); } catch { }
            }
        }
        else
        {
            LogError($"Download incomplete: {meta.Segments.Count(s => s.Completed)}/{meta.Segments.Count} segments");
        }
    }

    private async Task<DownloadMetadata> CreateNewDownload(string url, string filePath, int segmentsCount, 
        string metaPath, CancellationToken token)
    {
        var total = await GetContentLengthAsync(url, token);
        if (total <= 0)
            throw new InvalidOperationException("Server does not support resumable downloads");
        
        segmentsCount = CalculateOptimalSegments(total, segmentsCount);
        LogDebug($"Using {segmentsCount} segments for {total / (1024.0 * 1024.0):F2} MB");
        
        var meta = new DownloadMetadata { Url = url, FilePath = filePath, TotalSize = total };
        long partSize = total / segmentsCount;
        long currentPos = 0;
        
        for (int i = 0; i < segmentsCount; i++)
        {
            long from = currentPos;
            long to = (i == segmentsCount - 1) ? total - 1 : currentPos + partSize - 1;
            meta.Segments.Add(new DownloadSegment { From = from, To = to });
            currentPos = to + 1;
        }
        
        await File.WriteAllTextAsync(metaPath, JsonSerializer.Serialize(meta), token);
        
        LogDebug("Pre-allocating file...");
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 
                                     bufferSize: _bufferSize, FileOptions.WriteThrough);
        fs.SetLength(total);
        
        return meta;
    }

    private async Task MonitorProgressAsync(DownloadMetadata meta, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                var downloaded = meta.Segments.Sum(s => s.Downloaded);
                
                // Update progress frequently (every 100ms)
                if (now - _lastProgressUpdate >= ProgressInterval)
                {
                    _lastProgressUpdate = now;
                    Progress?.Invoke(downloaded, meta.TotalSize);
                }
                
                // Calculate speed with moving average (every 500ms)
                var timeSinceLastSample = (now - _lastSpeedSample).TotalSeconds;
                if (timeSinceLastSample >= 0.5)
                {
                    var bytesDiff = downloaded - _lastSpeedBytes;
                    
                    if (bytesDiff > 0 && timeSinceLastSample > 0)
                    {
                        // Calculate instantaneous speed
                        var instantSpeed = bytesDiff / timeSinceLastSample;
                        
                        // Add to samples queue
                        lock (_speedSamples)
                        {
                            _speedSamples.Enqueue(new SpeedSample 
                            { 
                                Time = now, 
                                BytesPerSecond = instantSpeed 
                            });
                            
                            // Keep only recent samples (last 5 seconds)
                            while (_speedSamples.Count > MaxSpeedSamples)
                            {
                                _speedSamples.Dequeue();
                            }
                            
                            // Calculate moving average for smooth display
                            // Give more weight to recent samples (weighted average)
                            if (_speedSamples.Count > 0)
                            {
                                var samples = _speedSamples.ToList();
                                double weightedSum = 0;
                                double weightTotal = 0;
                                
                                for (int i = 0; i < samples.Count; i++)
                                {
                                    // Recent samples get more weight (1, 2, 3, 4, 5...)
                                    double weight = i + 1;
                                    weightedSum += samples[i].BytesPerSecond * weight;
                                    weightTotal += weight;
                                }
                                
                                var smoothSpeed = weightedSum / weightTotal;
                                Speed?.Invoke(smoothSpeed);
                            }
                        }
                    }
                    
                    _lastSpeedBytes = downloaded;
                    _lastSpeedSample = now;
                }
                
                await Task.Delay(100, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch { }
        }
    }

    private int CalculateOptimalSegments(long fileSize, int requestedSegments)
    {
        var optimal = fileSize switch
        {
            < 1024 * 1024 => 1,
            < 10 * 1024 * 1024 => 4,
            < 50 * 1024 * 1024 => 8,
            < 100 * 1024 * 1024 => 12,
            < 500 * 1024 * 1024 => 16,
            _ => 16
        };
        
        return Math.Min(optimal, requestedSegments);
    }

    private async Task DownloadSegmentAsync(DownloadMetadata meta, DownloadSegment seg, int segmentIndex,
        string metaPath, CancellationToken token)
    {
        const int maxRetries = 5;
        int attempt = 0;
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

                    using var request = new HttpRequestMessage(HttpMethod.Get, meta.Url);
                    request.Headers.Range = new RangeHeaderValue(start, end);
                    request.Headers.ConnectionClose = false;
                    
                    using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        LogError($"Segment {segmentIndex} HTTP {(int)response.StatusCode}");
                        throw new HttpRequestException($"HTTP {response.StatusCode}");
                    }

                    await using var stream = await response.Content.ReadAsStreamAsync(token);
                    await using var fs = new FileStream(meta.FilePath, FileMode.Open, FileAccess.Write, 
                        FileShare.ReadWrite, _bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
                    
                    fs.Seek(start, SeekOrigin.Begin);

                    int read;
                    long segmentBytesRead = 0;
                    long lastMetadataSave = 0;
                    
                    while ((read = await stream.ReadAsync(buffer.AsMemory(0, _bufferSize), token)) > 0)
                    {
                        await fs.WriteAsync(buffer.AsMemory(0, read), token);
                        
                        lock (_progressLock)
                        {
                            seg.Downloaded += read;
                        }
                        
                        segmentBytesRead += read;

                        // Save metadata every 5MB
                        if (segmentBytesRead - lastMetadataSave >= 5 * 1024 * 1024)
                        {
                            try
                            {
                                var json = JsonSerializer.Serialize(meta);
                                await File.WriteAllTextAsync(metaPath, json, CancellationToken.None);
                                lastMetadataSave = segmentBytesRead;
                            }
                            catch { }
                        }
                    }
                    
                    await fs.FlushAsync(token);
                    
                    if (seg.Completed)
                    {
                        try
                        {
                            var json = JsonSerializer.Serialize(meta);
                            await File.WriteAllTextAsync(metaPath, json, CancellationToken.None);
                        }
                        catch { }
                    }
                    
                    break;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (attempt < maxRetries && !token.IsCancellationRequested)
                    {
                        var delay = (int)(1000 * Math.Pow(2, attempt - 1) + Random.Shared.Next(100, 1000));
                        LogError($"Segment {segmentIndex} attempt {attempt} failed: {ex.Message}");
                        await Task.Delay(delay, CancellationToken.None);
                    }
                    else
                    {
                        throw;
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

    public void Dispose()
    {
        if (_disposed) return;
        
        if (_ownsHttpClient)
        {
            _http?.Dispose();
        }
        
        _disposed = true;
    }
}

internal static class HttpClientManager
{
    private static readonly Lazy<HttpClient> _instance = new Lazy<HttpClient>(() =>
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 32,
            EnableMultipleHttp2Connections = false,
            ConnectTimeout = TimeSpan.FromSeconds(30),
            ResponseDrainTimeout = TimeSpan.FromSeconds(10)
        };
        
        return new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(30),
            DefaultRequestVersion = new Version(1, 1)
        };
    });

    public static HttpClient Instance => _instance.Value;
}