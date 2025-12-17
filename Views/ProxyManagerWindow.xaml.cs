using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MyFastDownloader.App.Models.Proxy;
using MyFastDownloader.App.Services.Proxy;
using MyFastDownloader.App.ViewModels;

namespace MyFastDownloader.App.Views;

/// <summary>
/// Interaction logic for ProxyManagerWindow.xaml
/// </summary>
public partial class ProxyManagerWindow : Window
{
    private readonly ProxyViewModel _viewModel;
    
    public ProxyManagerWindow()
    {
        InitializeComponent();
        
        var proxyManager = App.GetRequiredService<ProxyManager>();
        _viewModel = new ProxyViewModel(proxyManager);
        
        DataContext = _viewModel;
        
        Loaded += ProxyManagerWindow_Loaded;
    }
    
    private async void ProxyManagerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadProxiesAsync();
    }
    
    /// <summary>
    /// Loads all proxy configs
    /// </summary>
    private async System.Threading.Tasks.Task LoadProxiesAsync()
    {
        try
        {
            await _viewModel.LoadConfigsAsync();
            
            // Bind to ListView
            ProxiesListView.ItemsSource = _viewModel.Configs;
            
            UpdateUI();
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"✗ Lỗi: {ex.Message}";
            MessageBox.Show($"Không thể tải proxy configs: {ex.Message}", 
                          "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Updates UI based on current state
    /// </summary>
    private void UpdateUI()
    {
        var count = _viewModel.Configs.Count;
        
        SubtitleTextBlock.Text = count == 0 
            ? "Không có proxy" 
            : count == 1 
                ? "1 proxy configuration" 
                : $"{count} proxy configurations";
        
        EmptyStatePanel.Visibility = count == 0 
            ? Visibility.Visible 
            : Visibility.Collapsed;
        
        ProxiesListView.Visibility = count == 0 
            ? Visibility.Collapsed 
            : Visibility.Visible;
    }
    
    /// <summary>
    /// Handles add button click
    /// </summary>
    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var proxyManager = App.GetRequiredService<ProxyManager>();
            var dialog = new ProxySettingsDialog(proxyManager)
            {
                Owner = this
            };
            
            if (dialog.ShowDialog() == true && dialog.ResultConfig != null)
            {
                var success = await _viewModel.AddConfigAsync(dialog.ResultConfig);
                if (success)
                {
                    UpdateUI();
                    StatusTextBlock.Text = _viewModel.StatusMessage;
                }
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"✗ Lỗi: {ex.Message}";
            MessageBox.Show($"Không thể thêm proxy: {ex.Message}", 
                          "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Handles edit button click
    /// </summary>
    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ProxyConfig config)
        {
            try
            {
                var proxyManager = App.GetRequiredService<ProxyManager>();
                var dialog = new ProxySettingsDialog(proxyManager, config)
                {
                    Owner = this
                };
                
                if (dialog.ShowDialog() == true && dialog.ResultConfig != null)
                {
                    var success = await _viewModel.UpdateConfigAsync(dialog.ResultConfig);
                    if (success)
                    {
                        // Refresh ListView
                        ProxiesListView.Items.Refresh();
                        UpdateUI();
                        StatusTextBlock.Text = _viewModel.StatusMessage;
                    }
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"✗ Lỗi: {ex.Message}";
                MessageBox.Show($"Không thể cập nhật proxy: {ex.Message}", 
                              "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    /// <summary>
    /// Handles delete button click
    /// </summary>
    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ProxyConfig config)
        {
            try
            {
                var success = await _viewModel.DeleteConfigAsync(config);
                if (success)
                {
                    UpdateUI();
                    StatusTextBlock.Text = _viewModel.StatusMessage;
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"✗ Lỗi: {ex.Message}";
                MessageBox.Show($"Không thể xóa proxy: {ex.Message}", 
                              "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    /// <summary>
    /// Handles toggle button click
    /// </summary>
    private async void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ProxyConfig config)
        {
            try
            {
                await _viewModel.ToggleConfigAsync(config);
                
                // Refresh ListView
                ProxiesListView.Items.Refresh();
                StatusTextBlock.Text = _viewModel.StatusMessage;
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"✗ Lỗi: {ex.Message}";
            }
        }
    }
    
    /// <summary>
    /// Handles refresh button click
    /// </summary>
    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        StatusTextBlock.Text = "Đang tải lại...";
        await LoadProxiesAsync();
        StatusTextBlock.Text = "✓ Đã làm mới";
    }
    
    /// <summary>
    /// Handles search text changed
    /// </summary>
    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = SearchTextBox.Text.ToLowerInvariant();
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            ProxiesListView.ItemsSource = _viewModel.Configs;
        }
        else
        {
            var filtered = _viewModel.Configs.Where(p =>
                p.Name.ToLowerInvariant().Contains(searchText) ||
                p.Host.ToLowerInvariant().Contains(searchText) ||
                p.TypeText.ToLowerInvariant().Contains(searchText)
            ).ToList();
            
            ProxiesListView.ItemsSource = filtered;
        }
    }
    
    /// <summary>
    /// Handles proxy list selection changed
    /// </summary>
    private void ProxiesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProxiesListView.SelectedItem is ProxyConfig config)
        {
            _viewModel.SelectedConfig = config;
            StatusTextBlock.Text = $"Đã chọn: {config.DisplayText}";
        }
    }
    
    /// <summary>
    /// Handles close button click
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
