using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MyFastDownloader.App.Models;
using TaskStatus = MyFastDownloader.App.Models.TaskStatus;

namespace MyFastDownloader.App.Services;

public class DownloadManager
{
    private readonly ConcurrentDictionary<Guid, (DownloadTaskItem item, CancellationTokenSource cts)> _running = new();
    public event Action<DownloadTaskItem>? Updated;

    public void Add(DownloadTaskItem item) => Updated?.Invoke(item);
    
    public async Task StartAsync(DownloadTaskItem item)
    {
        if (_running.ContainsKey(item.Id)) return;
        var cts = new CancellationTokenSource();
        _running[item.Id] = (item, cts);

        item.Status = TaskStatus.Downloading;
        Updated?.Invoke(item);

        // USE MORE SEGMENTS FOR SPEED - let the downloader decide optimal count
        var engine = new SegmentedDownloader(maxParallel: 16); // Increased from 6 to 16
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
            // Use more segments by default
            var segments = Math.Max(item.SegmentsCount, 16);
            await engine.StartAsync(item.Url, item.FilePath, segments, cts.Token);
            
            if (item.Downloaded >= item.TotalSize && item.TotalSize > 0)
                item.Status = TaskStatus.Completed;
            else if (item.Status != TaskStatus.Paused)
                item.Status = TaskStatus.Error;
        }
        catch (OperationCanceledException)
        {
            item.Status = TaskStatus.Paused;
        }
        catch (Exception)
        {
            item.Status = TaskStatus.Error;
            item.SpeedBytesPerSec = 0;
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
            item.Status = TaskStatus.Paused;
            tup.cts.Cancel();
            Updated?.Invoke(item);
        }
    }

    public bool IsRunning(DownloadTaskItem item) => _running.ContainsKey(item.Id);
}