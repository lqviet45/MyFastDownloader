namespace MyFastDownloader.App.Models.Enums;

public enum TaskStatus
{
    Queued,
    Downloading,
    Paused,
    Completed,
    Error,
    Canceled
}