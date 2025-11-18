using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MyFastDownloader.App.Models.Enums;
using MyFastDownloader.App.Models.Core;
using TaskStatus = MyFastDownloader.App.Models.Enums.TaskStatus;
using MyFastDownloader.App.Services.Storage;
using MyFastDownloader.App.Services.Network;

namespace MyFastDownloader.App.Services.Core;

public class DownloadManager
{
    private readonly ConcurrentDictionary<Guid, (DownloadTaskItem item, CancellationTokenSource cts)> _running = new();
    private readonly SettingsService _settingsService;
    
    public event Action<DownloadTaskItem>? Updated;

    public DownloadManager() : this(new SettingsService())
    {
    }

    public DownloadManager(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _ = InitializeGlobalSpeedLimitAsync();
    }
    
    private async Task InitializeGlobalSpeedLimitAsync()
    {
        try
        {
            var settings = await _settingsService.LoadSettingsAsync();
            GlobalSpeedThrottler.Instance.Configure(
                settings.EnableSpeedLimit, 
                settings.GlobalSpeedLimitBytesPerSec
            );
            
            LogDebug($"Global speed limit initialized: {(settings.EnableSpeedLimit ? $"{settings.GlobalSpeedLimitKBps:F1} KB/s" : "Disabled")}");
        }
        catch (Exception ex)
        {
            LogError("Failed to initialize global speed limit", ex);
        }
    }
    
    /// <summary>
    /// Update global speed limit settings
    /// </summary>
    public async Task UpdateGlobalSpeedLimitAsync()
    {
        await InitializeGlobalSpeedLimitAsync();
    }

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
        LogDebug($"Speed limit mode: {item.SpeedLimitMode}");

        var maxParallel = Math.Min(item.SegmentsCount, 8);
        var engine = new SegmentedDownloader(maxParallel: maxParallel);
        
        // Configure speed limiting based on item's SpeedLimitMode
        switch (item.SpeedLimitMode)
        {
            case SpeedLimitMode.Unlimited:
                engine.DisableSpeedLimit();
                LogDebug("Speed limiting: Disabled");
                break;
                
            case SpeedLimitMode.Global:
                engine.UseGlobalSpeedLimit();
                var globalLimit = GlobalSpeedThrottler.Instance.GetSpeedLimit();
                LogDebug($"Speed limiting: Global ({globalLimit / 1024.0:F1} KB/s)");
                break;
                
            case SpeedLimitMode.Custom:
                engine.SetCustomSpeedLimit(item.CustomSpeedLimitBytesPerSec);
                LogDebug($"Speed limiting: Custom ({item.CustomSpeedLimitKBps:F1} KB/s)");
                break;
        }
        
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
            engine.Dispose();
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