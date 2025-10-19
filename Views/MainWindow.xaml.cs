using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MyFastDownloader.App.ViewModels;

namespace MyFastDownloader.App.Views
{
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
            if (sender is Button button && button.Tag is DownloadItemViewModel download)
            {
                if (download.Status == DownloadStatus.Downloading)
                {
                    download.Pause();
                }
                else if (download.Status == DownloadStatus.Paused)
                {
                    download.Resume();
                }
            }
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is DownloadItemViewModel download)
            {
                download.OpenFile();
            }
        }
    }
}
