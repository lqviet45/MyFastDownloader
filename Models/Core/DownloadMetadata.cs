namespace MyFastDownloader.App.Models.Core;

public class DownloadMetadata
{
    public string Url { get; set; } = "";
    public string FilePath { get; set; } = "";
    public long TotalSize { get; set; }
    public List<DownloadSegment> Segments { get; set; } = new();
}