using System;
using System.IO;
using System.Threading.Tasks;
using MyFastDownloader.App.Helpers.IO;
using MyFastDownloader.App.Models.Settings;
using MyFastDownloader.App.Services.Core;
using MyFastDownloader.App.Services.Network;
using MyFastDownloader.App.Services.Storage;
using MyFastDownloader.App.ViewModels.Base;

namespace MyFastDownloader.App.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly DownloadManager _downloadManager;
    private AppSettings _settings = new();

    public SettingsViewModel(SettingsService settingsService, DownloadManager downloadManager)
    {
        _settingsService = settingsService;
        _downloadManager = downloadManager;
    }

    public string DefaultDownloadFolder
    {
        get => _settings.DefaultDownloadFolder;
        set
        {
            if (_settings.DefaultDownloadFolder != value)
            {
                _settings.DefaultDownloadFolder = value;
                OnPropertyChanged();
            }
        }
    }

    public bool AlwaysAskSaveLocation
    {
        get => _settings.AlwaysAskSaveLocation;
        set
        {
            if (_settings.AlwaysAskSaveLocation != value)
            {
                _settings.AlwaysAskSaveLocation = value;
                OnPropertyChanged();
            }
        }
    }

    public int DefaultSegmentCount
    {
        get => _settings.DefaultSegmentCount;
        set
        {
            if (_settings.DefaultSegmentCount != value)
            {
                _settings.DefaultSegmentCount = value;
                OnPropertyChanged();
            }
        }
    }

    public int MaxConcurrentDownloads
    {
        get => _settings.MaxConcurrentDownloads;
        set
        {
            if (_settings.MaxConcurrentDownloads != value)
            {
                _settings.MaxConcurrentDownloads = value;
                OnPropertyChanged();
            }
        }
    }

    public bool EnableSpeedLimit
    {
        get => _settings.EnableSpeedLimit;
        set
        {
            if (_settings.EnableSpeedLimit != value)
            {
                _settings.EnableSpeedLimit = value;
                OnPropertyChanged();
            }
        }
    }

    public double GlobalSpeedLimitKBps
    {
        get => _settings.GlobalSpeedLimitKBps;
        set
        {
            if (Math.Abs(_settings.GlobalSpeedLimitKBps - value) > double.Epsilon)
            {
                _settings.GlobalSpeedLimitKBps = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GlobalSpeedLimitBytesPerSec));
            }
        }
    }

    public long GlobalSpeedLimitBytesPerSec => _settings.GlobalSpeedLimitBytesPerSec;

    public async Task InitializeAsync()
    {
        _settings = await _settingsService.LoadSettingsAsync();
        RaiseAllProperties();
    }

    public void ApplySpeedPreset(double speedKbPerSec)
    {
        GlobalSpeedLimitKBps = speedKbPerSec;
    }

    public void ResetToDefaultFolder()
    {
        DefaultDownloadFolder = FileHelper.GetDefaultDownloadsFolder();
    }

    public async Task SaveAsync()
    {
        if (!Directory.Exists(DefaultDownloadFolder))
        {
            Directory.CreateDirectory(DefaultDownloadFolder);
        }

        if (EnableSpeedLimit && GlobalSpeedLimitKBps < 10)
        {
            throw new InvalidOperationException("Giới hạn tốc độ phải ít nhất 10 KB/s");
        }

        await _settingsService.SaveSettingsAsync(_settings);

        GlobalSpeedThrottler.Instance.Configure(
            EnableSpeedLimit,
            GlobalSpeedLimitBytesPerSec);

        await _downloadManager.UpdateGlobalSpeedLimitAsync();
    }

    private void RaiseAllProperties()
    {
        OnPropertyChanged(nameof(DefaultDownloadFolder));
        OnPropertyChanged(nameof(AlwaysAskSaveLocation));
        OnPropertyChanged(nameof(DefaultSegmentCount));
        OnPropertyChanged(nameof(MaxConcurrentDownloads));
        OnPropertyChanged(nameof(EnableSpeedLimit));
        OnPropertyChanged(nameof(GlobalSpeedLimitKBps));
    }
}

