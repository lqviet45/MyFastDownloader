using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MyFastDownloader.App.Models;
using MyFastDownloader.App.ViewModels;
using TaskStatus = MyFastDownloader.App.Models.TaskStatus;

namespace MyFastDownloader.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.AddDownloadAsync();
    }

    private void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _ = _viewModel.AddDownloadAsync();
        }
    }

    private void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DownloadTaskItem item)
        {
            if (item.Status == TaskStatus.Downloading)
            {
                _viewModel.PauseDownload(item);
            }
            else if (item.Status == TaskStatus.Paused || item.Status == TaskStatus.Error)
            {
                _viewModel.StartDownload(item);
            }
        }
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DownloadTaskItem item)
        {
            if (item.Status == TaskStatus.Completed && File.Exists(item.FilePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{item.FilePath}\"",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể mở file: {ex.Message}", "Lỗi", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}