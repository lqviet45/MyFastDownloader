using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace MyFastDownloader.App.Models;

public class DownloadTaskItem : INotifyPropertyChanged
{
    public Guid Id { get; } = Guid.NewGuid();
    
    private string _url = "";
    public string Url 
    { 
        get => _url; 
        set { _url = value; OnPropertyChanged(); } 
    }
    
    private string _filePath = "";
    public string FilePath 
    { 
        get => _filePath; 
        set { _filePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(FileName)); } 
    }
    
    private long _totalSize;
    public long TotalSize 
    { 
        get => _totalSize; 
        set { _totalSize = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressPercent)); OnPropertyChanged(nameof(SizeText)); } 
    }
    
    private long _downloaded;
    public long Downloaded 
    { 
        get => _downloaded; 
        set { _downloaded = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressPercent)); OnPropertyChanged(nameof(SizeText)); } 
    }
    
    private TaskStatus _status = TaskStatus.Queued;
    public TaskStatus Status 
    { 
        get => _status; 
        set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusText)); OnPropertyChanged(nameof(StatusColor)); OnPropertyChanged(nameof(StatusForeground)); } 
    }
    
    private double _speedBytesPerSec;
    public double SpeedBytesPerSec 
    { 
        get => _speedBytesPerSec; 
        set { _speedBytesPerSec = value; OnPropertyChanged(); OnPropertyChanged(nameof(SpeedText)); } 
    }
    
    public int SegmentsCount { get; set; } = 6;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    
    // Speed limiting properties
    private SpeedLimitMode _speedLimitMode = SpeedLimitMode.Global;
    public SpeedLimitMode SpeedLimitMode
    {
        get => _speedLimitMode;
        set { _speedLimitMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(SpeedLimitText)); }
    }
    
    private long _customSpeedLimitBytesPerSec = 0;
    public long CustomSpeedLimitBytesPerSec
    {
        get => _customSpeedLimitBytesPerSec;
        set { _customSpeedLimitBytesPerSec = value; OnPropertyChanged(); OnPropertyChanged(nameof(SpeedLimitText)); }
    }

    // UI Properties
    public string FileName => string.IsNullOrEmpty(FilePath) ? "Unknown" : Path.GetFileName(FilePath);
    public double ProgressPercent => TotalSize > 0 ? Math.Round((double)Downloaded / TotalSize * 100, 1) : 0;
    public string SizeText => TotalSize > 0 ? $"{FormatBytes(Downloaded)} / {FormatBytes(TotalSize)}" : "Unknown size";
    public string SpeedText => SpeedBytesPerSec > 0 ? $"{FormatBytes(SpeedBytesPerSec)}/s" : "";
    
    public string SpeedLimitText => SpeedLimitMode switch
    {
        SpeedLimitMode.Unlimited => "Không giới hạn",
        SpeedLimitMode.Global => "Global",
        SpeedLimitMode.Custom => $"⚡ {FormatBytes(CustomSpeedLimitBytesPerSec)}/s",
        _ => ""
    };
    
    public string StatusText => Status switch
    {
        TaskStatus.Queued => "Chờ",
        TaskStatus.Downloading => "Đang tải",
        TaskStatus.Paused => "Tạm dừng",
        TaskStatus.Completed => "Hoàn thành",
        TaskStatus.Error => "Lỗi",
        TaskStatus.Canceled => "Hủy",
        _ => "Unknown"
    };

    public SolidColorBrush StatusColor => Status switch
    {
        TaskStatus.Queued => new SolidColorBrush(Color.FromRgb(75, 85, 99)),
        TaskStatus.Downloading => new SolidColorBrush(Color.FromRgb(16, 185, 129)),
        TaskStatus.Paused => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
        TaskStatus.Completed => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
        TaskStatus.Error => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
        TaskStatus.Canceled => new SolidColorBrush(Color.FromRgb(107, 114, 128)),
        _ => new SolidColorBrush(Color.FromRgb(75, 85, 99))
    };

    public SolidColorBrush StatusForeground => new SolidColorBrush(Colors.White);
    
    /// <summary>
    /// Helper to get custom speed limit in KB/s
    /// </summary>
    public double CustomSpeedLimitKBps
    {
        get => CustomSpeedLimitBytesPerSec / 1024.0;
        set => CustomSpeedLimitBytesPerSec = (long)(value * 1024);
    }
    
    /// <summary>
    /// Helper to get custom speed limit in MB/s
    /// </summary>
    public double CustomSpeedLimitMBps
    {
        get => CustomSpeedLimitBytesPerSec / (1024.0 * 1024.0);
        set => CustomSpeedLimitBytesPerSec = (long)(value * 1024 * 1024);
    }

    private static string FormatBytes(double bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        while (bytes >= 1024 && counter < suffixes.Length - 1)
        {
            bytes /= 1024;
            counter++;
        }
        return $"{bytes:F1} {suffixes[counter]}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}