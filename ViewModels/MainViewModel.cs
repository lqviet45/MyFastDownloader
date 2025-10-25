using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using MyFastDownloader.App.Models;
using MyFastDownloader.App.Services;
using TaskStatus = MyFastDownloader.App.Models.TaskStatus;

namespace MyFastDownloader.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private string _downloadUrl = "";
    private string _statusMessage = "";
    private readonly DownloadManager _downloadManager;

    public ObservableCollection<DownloadTaskItem> Downloads { get; set; }

    public string DownloadUrl
    {
        get => _downloadUrl;
        set
        {
            _downloadUrl = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public string DownloadStats
    {
        get
        {
            var downloading = Downloads.Count(d => d.Status == TaskStatus.Downloading);
            var completed = Downloads.Count(d => d.Status == TaskStatus.Completed);
            return $"{downloading} đang tải • {completed} hoàn thành";
        }
    }

    public Visibility EmptyStateVisibility => Downloads.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public MainViewModel()
    {
        Downloads = new ObservableCollection<DownloadTaskItem>();
        Downloads.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(DownloadStats));
            OnPropertyChanged(nameof(EmptyStateVisibility));
        };

        _downloadManager = App.GetDownloadManager() ?? new DownloadManager();
        _downloadManager.Updated += OnDownloadUpdated;
    }

    private void OnDownloadUpdated(DownloadTaskItem item)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            OnPropertyChanged(nameof(DownloadStats));
        });
    }

    public async Task AddDownloadAsync()
    {
        if (string.IsNullOrWhiteSpace(DownloadUrl))
        {
            MessageBox.Show("Vui lòng nhập URL!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var fileName = GetFileNameFromUrl(DownloadUrl);
            
            var saveDialog = new SaveFileDialog
            {
                FileName = fileName,
                Filter = "All Files (*.*)|*.*",
                Title = "Chọn vị trí lưu file",
                InitialDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads")
            };
            
            if (saveDialog.ShowDialog() != true)
                return;

            var item = new DownloadTaskItem
            {
                Url = DownloadUrl,
                FilePath = saveDialog.FileName,
                SegmentsCount = 32, // INCREASED from 6 to 32 for maximum speed
                Status = TaskStatus.Queued
            };
            
            Downloads.Insert(0, item);
            DownloadUrl = string.Empty;
            StatusMessage = $"Đã thêm: {item.FileName}";

            _ = _downloadManager.StartAsync(item);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string GetFileNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var fileName = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrEmpty(fileName) || fileName == "/")
                fileName = $"download_{DateTime.Now:yyyyMMddHHmmss}";
            return fileName;
        }
        catch
        {
            return $"download_{DateTime.Now:yyyyMMddHHmmss}";
        }
    }

    public void StartDownload(DownloadTaskItem item)
    {
        if (item.Status == TaskStatus.Paused || item.Status == TaskStatus.Error)
        {
            _ = _downloadManager.StartAsync(item);
        }
    }

    public void PauseDownload(DownloadTaskItem item)
    {
        _downloadManager.Pause(item);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}