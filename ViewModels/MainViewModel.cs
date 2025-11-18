using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using MyFastDownloader.App.Helpers;
using MyFastDownloader.App.Models.Core;
using MyFastDownloader.App.Models.Enums;
using MyFastDownloader.App.Models.Settings;
using MyFastDownloader.App.Services.Core;
using MyFastDownloader.App.Services.Storage;
using MyFastDownloader.App.ViewModels.Base;
using TaskStatus = MyFastDownloader.App.Models.Enums.TaskStatus;

namespace MyFastDownloader.App.ViewModels;

/// <summary>
/// ViewModel for the main application window
/// </summary>
public class MainViewModel : ViewModelBase
{
    private string _downloadUrl = "";
    private string _statusMessage = "";
    private readonly DownloadManager _downloadManager;
    private readonly SettingsService _settingsService;
    private AppSettings _settings;

    public ObservableCollection<DownloadTaskItem> Downloads { get; set; }

    public string DownloadUrl
    {
        get => _downloadUrl;
        set => SetProperty(ref _downloadUrl, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
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
    
    public string DefaultDownloadFolder => _settings.DefaultDownloadFolder;

    public MainViewModel() : this(new DownloadManager(), new SettingsService())
    {
    }

    public MainViewModel(DownloadManager downloadManager, SettingsService settingsService)
    {
        Downloads = new ObservableCollection<DownloadTaskItem>();
        Downloads.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(DownloadStats));
            OnPropertyChanged(nameof(EmptyStateVisibility));
        };

        _downloadManager = downloadManager;
        _downloadManager.Updated += OnDownloadUpdated;
        
        // Initialize settings
        _settingsService = settingsService;
        _settings = new AppSettings();
        _ = LoadSettingsAsync();
    }
    
    private async Task LoadSettingsAsync()
    {
        _settings = await _settingsService.LoadSettingsAsync();
        OnPropertyChanged(nameof(DefaultDownloadFolder));
    }
    
    public async void ReloadSettings()
    {
        _settings = await _settingsService.LoadSettingsAsync();
        OnPropertyChanged(nameof(DefaultDownloadFolder));
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
        await Task.CompletedTask;
        if (string.IsNullOrWhiteSpace(DownloadUrl))
        {
            MessageBox.Show("Vui lòng nhập URL!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!ValidationHelper.IsValidUrl(DownloadUrl))
        {
            MessageBox.Show("URL không hợp lệ. Vui lòng kiểm tra lại!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var fileName = GetFileNameFromUrl(DownloadUrl);
            string filePath;
            
            // Check if should always ask or use default folder
            if (_settings.AlwaysAskSaveLocation)
            {
                // Show dialog
                var saveDialog = new SaveFileDialog
                {
                    FileName = fileName,
                    Filter = "All Files (*.*)|*.*",
                    Title = "Chọn vị trí lưu file",
                    InitialDirectory = _settings.DefaultDownloadFolder
                };
                
                if (saveDialog.ShowDialog() != true)
                    return;
                
                filePath = saveDialog.FileName;
            }
            else
            {
                // Use default folder
                Directory.CreateDirectory(_settings.DefaultDownloadFolder);
                filePath = Path.Combine(_settings.DefaultDownloadFolder, fileName);
                
                // Check if file exists and ask to overwrite
                if (File.Exists(filePath))
                {
                    var result = MessageBox.Show(
                        $"File '{fileName}' đã tồn tại. Bạn có muốn ghi đè?",
                        "File đã tồn tại",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.No)
                    {
                        // Show save dialog to choose different name
                        var saveDialog = new SaveFileDialog
                        {
                            FileName = Path.GetFileName(FileHelper.GetUniqueFilePath(filePath)),
                            Filter = "All Files (*.*)|*.*",
                            Title = "Chọn vị trí lưu file",
                            InitialDirectory = _settings.DefaultDownloadFolder
                        };
                        
                        if (saveDialog.ShowDialog() != true)
                            return;
                        
                        filePath = saveDialog.FileName;
                    }
                }
            }

            FileHelper.EnsureDirectoryExists(filePath);

            var item = new DownloadTaskItem
            {
                Url = DownloadUrl,
                FilePath = filePath,
                SegmentsCount = _settings.DefaultSegmentCount,
                Status = TaskStatus.Queued,
                SpeedLimitMode = _settings.EnableSpeedLimit && _settings.GlobalSpeedLimitBytesPerSec > 0 
                    ? SpeedLimitMode.Global 
                    : SpeedLimitMode.Unlimited
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

            if (string.IsNullOrWhiteSpace(fileName) || fileName == "/")
            {
                var extension = UriHelper.GetFileExtension(url);
                fileName = $"download_{DateTime.Now:yyyyMMddHHmmss}{extension}";
            }

            return ValidationHelper.SanitizeFileName(fileName);
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
}