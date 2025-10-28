using System.IO;

namespace MyFastDownloader.App.Models;

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
    /// Gets the default Windows Downloads folder
    /// </summary>
    private static string GetDefaultDownloadsFolder()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");
    }
}