using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;

namespace MyFastDownloader.App.ViewModels;

public class DownloadItemViewModel : INotifyPropertyChanged
{
    private readonly string _url;
    private readonly MainViewModel _mainViewModel;
    private string _fileName;
    private long _totalSize;
    private long _downloadedSize;
    private DownloadStatus _status;
    private double _speed;
    private string _filePath;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly string _downloadFolder;

    public Guid Id { get; }
    public DateTime CreatedAt { get; }

    public string FileName
    {
        get => _fileName;
        set
        {
            _fileName = value;
            OnPropertyChanged(nameof(FileName));
        }
    }

    public long TotalSize
    {
        get => _totalSize;
        set
        {
            _totalSize = value;
            OnPropertyChanged(nameof(TotalSize));
            OnPropertyChanged(nameof(FileSizeText));
        }
    }

    public long DownloadedSize
    {
        get => _downloadedSize;
        set
        {
            _downloadedSize = value;
            OnPropertyChanged(nameof(DownloadedSize));
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(ProgressText));
        }
    }

    public DownloadStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(StatusBackground));
            OnPropertyChanged(nameof(StatusBorder));
            OnPropertyChanged(nameof(ActionButtonIcon));
            OnPropertyChanged(nameof(ActionButtonBackground));
            OnPropertyChanged(nameof(ActionButtonVisibility));
            OnPropertyChanged(nameof(OpenButtonVisibility));
            _mainViewModel?.UpdateStats();
        }
    }

    public double Speed
    {
        get => _speed;
        set
        {
            _speed = value;
            OnPropertyChanged(nameof(Speed));
            OnPropertyChanged(nameof(SpeedText));
        }
    }

    public double Progress => TotalSize > 0 ? (DownloadedSize * 100.0 / TotalSize) : 0;

    public string ProgressText => $"{Progress:F0}%";

    public string FileSizeText => FormatBytes(TotalSize);

    public string SpeedText => Status == DownloadStatus.Downloading ? $"{FormatBytes((long)Speed)}/s" : "0 KB/s";

    public string StatusText => Status switch
    {
        DownloadStatus.Downloading => "Đang tải",
        DownloadStatus.Completed => "Hoàn thành",
        DownloadStatus.Paused => "Tạm dừng",
        DownloadStatus.Failed => "Thất bại",
        _ => "Đang chờ"
    };

    public Brush StatusColor => Status switch
    {
        DownloadStatus.Downloading => new SolidColorBrush(Color.FromRgb(96, 165, 250)),
        DownloadStatus.Completed => new SolidColorBrush(Color.FromRgb(52, 211, 153)),
        DownloadStatus.Paused => new SolidColorBrush(Color.FromRgb(251, 191, 36)),
        DownloadStatus.Failed => new SolidColorBrush(Color.FromRgb(248, 113, 113)),
        _ => new SolidColorBrush(Color.FromRgb(148, 163, 184))
    };

    public Brush StatusBackground => Status switch
    {
        DownloadStatus.Downloading => new SolidColorBrush(Color.FromArgb(51, 59, 130, 246)),
        DownloadStatus.Completed => new SolidColorBrush(Color.FromArgb(51, 16, 185, 129)),
        DownloadStatus.Paused => new SolidColorBrush(Color.FromArgb(51, 245, 158, 11)),
        DownloadStatus.Failed => new SolidColorBrush(Color.FromArgb(51, 239, 68, 68)),
        _ => new SolidColorBrush(Color.FromArgb(51, 148, 163, 184))
    };

    public Brush StatusBorder => Status switch
    {
        DownloadStatus.Downloading => new SolidColorBrush(Color.FromArgb(77, 59, 130, 246)),
        DownloadStatus.Completed => new SolidColorBrush(Color.FromArgb(77, 16, 185, 129)),
        DownloadStatus.Paused => new SolidColorBrush(Color.FromArgb(77, 245, 158, 11)),
        DownloadStatus.Failed => new SolidColorBrush(Color.FromArgb(77, 239, 68, 68)),
        _ => new SolidColorBrush(Color.FromArgb(77, 148, 163, 184))
    };

    public string ActionButtonIcon => Status == DownloadStatus.Downloading ? "⏸" : "▶";

    public Brush ActionButtonBackground => Status == DownloadStatus.Downloading
        ? CreateGradientBrush(Color.FromRgb(245, 158, 11), Color.FromRgb(217, 119, 6))
        : CreateGradientBrush(Color.FromRgb(16, 185, 129), Color.FromRgb(5, 150, 105));

    public Visibility ActionButtonVisibility =>
        Status == DownloadStatus.Downloading || Status == DownloadStatus.Paused
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility OpenButtonVisibility =>
        Status == DownloadStatus.Completed ? Visibility.Visible : Visibility.Collapsed;

    public DownloadItemViewModel(string url, MainViewModel mainViewModel)
    {
        Id = Guid.NewGuid();
        _url = url;
        _mainViewModel = mainViewModel;
        CreatedAt = DateTime.Now;
        FileName = GetFileNameFromUrl(url);
        Status = DownloadStatus.Queued;

        _downloadFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "MyFastDownloader"
        );
        Directory.CreateDirectory(_downloadFolder);
    }

    public async Task StartDownloadAsync()
    {
        if (Status == DownloadStatus.Downloading) return;

        _cancellationTokenSource = new CancellationTokenSource();
        Status = DownloadStatus.Downloading;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromHours(1) };
            using var response = await client.GetAsync(_url, HttpCompletionOption.ResponseHeadersRead,
                _cancellationTokenSource.Token);
            response.EnsureSuccessStatusCode();

            TotalSize = response.Content.Headers.ContentLength ?? 0;
            _filePath = Path.Combine(_downloadFolder, FileName);

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream =
                new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            var lastUpdate = DateTime.Now;
            var lastSize = DownloadedSize;

            while (true)
            {
                var read = await contentStream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);
                if (read == 0) break;

                await fileStream.WriteAsync(buffer, 0, read, _cancellationTokenSource.Token);
                DownloadedSize += read;

                var now = DateTime.Now;
                var elapsed = (now - lastUpdate).TotalSeconds;
                if (elapsed >= 0.5)
                {
                    Speed = (DownloadedSize - lastSize) / elapsed;
                    lastUpdate = now;
                    lastSize = DownloadedSize;
                }
            }

            Status = DownloadStatus.Completed;
            Speed = 0;
        }
        catch (OperationCanceledException)
        {
            if (Status != DownloadStatus.Paused)
            {
                Status = DownloadStatus.Failed;
            }

            Speed = 0;
        }
        catch (Exception)
        {
            Status = DownloadStatus.Failed;
            Speed = 0;
        }
    }

    public void Pause()
    {
        if (Status == DownloadStatus.Downloading)
        {
            _cancellationTokenSource?.Cancel();
            Status = DownloadStatus.Paused;
            Speed = 0;
        }
    }

    public async void Resume()
    {
        if (Status == DownloadStatus.Paused)
        {
            await StartDownloadAsync();
        }
    }

    public void OpenFile()
    {
        if (Status == DownloadStatus.Completed && File.Exists(_filePath))
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{_filePath}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private string GetFileNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var fileName = Path.GetFileName(uri.LocalPath);
            return string.IsNullOrEmpty(fileName) ? $"download_{DateTime.Now:yyyyMMddHHmmss}" : fileName;
        }
        catch
        {
            return $"download_{DateTime.Now:yyyyMMddHHmmss}";
        }
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    private LinearGradientBrush CreateGradientBrush(Color start, Color end)
    {
        return new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop(start, 0),
                new GradientStop(end, 1)
            }
        };
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// DownloadStatus.cs
public enum DownloadStatus
{
    Queued,
    Downloading,
    Paused,
    Completed,
    Failed
}