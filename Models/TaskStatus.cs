namespace MyFastDownloader.App.Models;

public enum TaskStatus
{
    Queued,
    Downloading,
    Paused,
    Completed,
    Error,
    Canceled
}