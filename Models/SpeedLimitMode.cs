namespace MyFastDownloader.App.Models;

/// <summary>
/// Speed limiting modes for downloads
/// </summary>
public enum SpeedLimitMode
{
    /// <summary>
    /// No speed limit applied
    /// </summary>
    Unlimited,
    
    /// <summary>
    /// Use global speed limit from settings
    /// </summary>
    Global,
    
    /// <summary>
    /// Use custom speed limit for this download only
    /// </summary>
    Custom
}