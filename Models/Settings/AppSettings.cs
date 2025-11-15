using System.IO;

namespace MyFastDownloader.App.Models.Settings;

/// <summary>
/// Application settings for persistence
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Default download folder path
    /// </summary>
    public string DefaultDownloadFolder { get; set; } = GetDefaultDownloadsFolder();
    
    /// <summary>
    /// Whether to always ask for save location
    /// </summary>
    public bool AlwaysAskSaveLocation { get; set; } = false;
    
    /// <summary>
    /// Number of parallel segments per download
    /// </summary>
    public int DefaultSegmentCount { get; set; } = 8;
    
    /// <summary>
    /// Maximum concurrent downloads
    /// </summary>
    public int MaxConcurrentDownloads { get; set; } = 3;
    
    /// <summary>
    /// Enable global speed limiting
    /// </summary>
    public bool EnableSpeedLimit { get; set; } = false;
    
    /// <summary>
    /// Global speed limit in bytes per second (0 = unlimited)
    /// </summary>
    public long GlobalSpeedLimitBytesPerSec { get; set; } = 0;
    
    /// <summary>
    /// Gets the default Windows Downloads folder
    /// </summary>
    private static string GetDefaultDownloadsFolder()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");
    }
    
    /// <summary>
    /// Helper to get speed limit in KB/s
    /// </summary>
    public double GlobalSpeedLimitKBps
    {
        get => GlobalSpeedLimitBytesPerSec / 1024.0;
        set => GlobalSpeedLimitBytesPerSec = (long)(value * 1024);
    }
    
    /// <summary>
    /// Helper to get speed limit in MB/s
    /// </summary>
    public double GlobalSpeedLimitMBps
    {
        get => GlobalSpeedLimitBytesPerSec / (1024.0 * 1024.0);
        set => GlobalSpeedLimitBytesPerSec = (long)(value * 1024 * 1024);
    }
}