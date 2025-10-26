using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MyFastDownloader.App.Models;
using TaskStatus = MyFastDownloader.App.Models.TaskStatus;

namespace MyFastDownloader.App.Services;

public class DownloadManager
{
    private readonly ConcurrentDictionary<Guid, (DownloadTaskItem item, CancellationTokenSource cts)> _running = new();
    public event Action<DownloadTaskItem>? Updated;

    private void LogDebug(string message)
    {
        Debug.WriteLine($"[DownloadManager] {DateTime.Now:HH:mm:ss.fff} - {message}");
    }
    
    private void LogError(string message, Exception? ex = null)
    {
        Debug.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss.fff} - {message}");
        Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss.fff} - {message}");
        if (ex != null)
        {
            Debug.WriteLine($"[ERROR] Exception: {ex.Message}\n{ex.StackTrace}");
            Console.WriteLine($"[ERROR] Exception: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void Add(DownloadTaskItem item) => Updated?.Invoke(item);
    
    public async Task StartAsync(DownloadTaskItem item)
    {
        if (_running.ContainsKey(item.Id))
        {
            LogDebug($"Download already running: {item.FileName}");
            return;
        }
        
        var cts = new CancellationTokenSource();
        _running[item.Id] = (item, cts);

        item.Status = TaskStatus.Downloading;
        Updated?.Invoke(item);

        LogDebug($"Starting download: {item.FileName}");
        LogDebug($"URL: {item.Url}");
        LogDebug($"File path: {item.FilePath}");
        LogDebug($"Segments: {item.SegmentsCount}");

        // Use reasonable number of parallel connections (8 is optimal for most cases)
        var maxParallel = Math.Min(item.SegmentsCount, 8);
        var engine = new SegmentedDownloader(maxParallel: maxParallel);
        
        engine.Progress += (dl, total) =>
        {
            item.TotalSize = total;
            item.Downloaded = dl;
            Updated?.Invoke(item);
        };
        
        engine.Speed += v =>
        {
            item.SpeedBytesPerSec = v;
            Updated?.Invoke(item);
        };

        try
        {
            await engine.StartAsync(item.Url, item.FilePath, item.SegmentsCount, cts.Token);
            
            if (item.Downloaded >= item.TotalSize && item.TotalSize > 0)
            {
                item.Status = TaskStatus.Completed;
                LogDebug($"Download completed: {item.FileName}");
            }
            else if (item.Status != TaskStatus.Paused)
            {
                item.Status = TaskStatus.Error;
                LogError($"Download incomplete: {item.FileName} - {item.Downloaded:N0}/{item.TotalSize:N0} bytes");
            }
        }
        catch (OperationCanceledException)
        {
            item.Status = TaskStatus.Paused;
            LogDebug($"Download paused: {item.FileName}");
        }
        catch (Exception ex)
        {
            item.Status = TaskStatus.Error;
            item.SpeedBytesPerSec = 0;
            LogError($"Download error: {item.FileName}", ex);
        }
        finally
        {
            Updated?.Invoke(item);
            _running.TryRemove(item.Id, out _);
        }
    }

    public void Pause(DownloadTaskItem item)
    {
        if (_running.TryGetValue(item.Id, out var tup))
        {
            LogDebug($"Pausing download: {item.FileName}");
            item.Status = TaskStatus.Paused;
            tup.cts.Cancel();
            Updated?.Invoke(item);
        }
    }

    public bool IsRunning(DownloadTaskItem item) => _running.ContainsKey(item.Id);
}