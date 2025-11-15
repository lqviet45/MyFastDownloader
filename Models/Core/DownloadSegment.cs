namespace MyFastDownloader.App.Models.Core;

public class DownloadSegment
{
    public long From { get; set; }
    public long To { get; set; }
    public long Downloaded { get; set; } = 0;
    public bool Completed => Downloaded >= (To - From + 1);
}