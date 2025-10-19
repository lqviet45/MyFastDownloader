using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using MyFastDownloader.App.Models;
using MyFastDownloader.App.Services;

namespace MyFastDownloader.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged
    {
        private string _downloadUrl;
        private string _statusMessage;

        public ObservableCollection<DownloadItemViewModel> Downloads { get; set; }

        public string DownloadUrl
        {
            get => _downloadUrl;
            set
            {
                _downloadUrl = value;
                OnPropertyChanged(nameof(DownloadUrl));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public string DownloadStats
        {
            get
            {
                var downloading = Downloads.Count(d => d.Status == DownloadStatus.Downloading);
                var completed = Downloads.Count(d => d.Status == DownloadStatus.Completed);
                return $"{downloading} đang tải • {completed} hoàn thành";
            }
        }

        public Visibility EmptyStateVisibility => Downloads.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        public MainViewModel()
        {
            Downloads = new ObservableCollection<DownloadItemViewModel>();
            Downloads.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(DownloadStats));
                OnPropertyChanged(nameof(EmptyStateVisibility));
            };
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
                var download = new DownloadItemViewModel(DownloadUrl, this);
                Downloads.Insert(0, download);
                DownloadUrl = string.Empty;
                StatusMessage = "Đã thêm tải xuống mới";
                await download.StartDownloadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void UpdateStats()
        {
            OnPropertyChanged(nameof(DownloadStats));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

