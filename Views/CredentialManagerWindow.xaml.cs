using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MyFastDownloader.App.Models.Auth;
using MyFastDownloader.App.Services.Auth;
using MyFastDownloader.App.ViewModels;

namespace MyFastDownloader.App.Views;

/// <summary>
/// Interaction logic for CredentialManagerWindow.xaml
/// </summary>
public partial class CredentialManagerWindow : Window
{
    private readonly CredentialViewModel _viewModel;
    
    public CredentialManagerWindow()
    {
        InitializeComponent();
        
        var credentialManager = App.GetRequiredService<CredentialManager>();
        _viewModel = new CredentialViewModel(credentialManager);
        
        DataContext = _viewModel;
        
        Loaded += CredentialManagerWindow_Loaded;
    }
    
    private async void CredentialManagerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadCredentialsAsync();
    }
    
    /// <summary>
    /// Loads all credentials
    /// </summary>
    private async System.Threading.Tasks.Task LoadCredentialsAsync()
    {
        try
        {
            await _viewModel.LoadCredentialsAsync();
            
            // Bind to ListView
            CredentialsListView.ItemsSource = _viewModel.Credentials;
            
            UpdateUI();
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"✗ Lỗi: {ex.Message}";
            MessageBox.Show($"Không thể tải credentials: {ex.Message}", 
                          "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Updates UI based on current state
    /// </summary>
    private void UpdateUI()
    {
        var count = _viewModel.Credentials.Count;
        
        SubtitleTextBlock.Text = count == 0 
            ? "Không có credentials" 
            : count == 1 
                ? "1 credential" 
                : $"{count} credentials";
        
        EmptyStatePanel.Visibility = count == 0 
            ? Visibility.Visible 
            : Visibility.Collapsed;
        
        CredentialsListView.Visibility = count == 0 
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
            var credentialManager = App.GetRequiredService<CredentialManager>();
            var dialog = new CredentialDialog(credentialManager)
            {
                Owner = this
            };
            
            if (dialog.ShowDialog() == true && dialog.ResultCredential != null)
            {
                var success = await _viewModel.AddCredentialAsync(dialog.ResultCredential);
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
            MessageBox.Show($"Không thể thêm credential: {ex.Message}", 
                          "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
    /// Handles edit button click
    /// </summary>
    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Credential credential)
        {
            try
            {
                var credentialManager = App.GetRequiredService<CredentialManager>();
                var dialog = new CredentialDialog(credentialManager, credential)
                {
                    Owner = this
                };
                
                if (dialog.ShowDialog() == true && dialog.ResultCredential != null)
                {
                    var success = await _viewModel.UpdateCredentialAsync(dialog.ResultCredential);
                    if (success)
                    {
                        // Refresh ListView
                        CredentialsListView.Items.Refresh();
                        UpdateUI();
                        StatusTextBlock.Text = _viewModel.StatusMessage;
                    }
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"✗ Lỗi: {ex.Message}";
                MessageBox.Show($"Không thể cập nhật credential: {ex.Message}", 
                              "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    /// <summary>
    /// Handles delete button click
    /// </summary>
    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Credential credential)
        {
            try
            {
                var success = await _viewModel.DeleteCredentialAsync(credential);
                if (success)
                {
                    UpdateUI();
                    StatusTextBlock.Text = _viewModel.StatusMessage;
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"✗ Lỗi: {ex.Message}";
                MessageBox.Show($"Không thể xóa credential: {ex.Message}", 
                              "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    /// <summary>
    /// Handles toggle button click
    /// </summary>
    private async void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Credential credential)
        {
            try
            {
                await _viewModel.ToggleCredentialAsync(credential);
                
                // Refresh ListView
                CredentialsListView.Items.Refresh();
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
        await LoadCredentialsAsync();
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
            CredentialsListView.ItemsSource = _viewModel.Credentials;
        }
        else
        {
            var filtered = _viewModel.Credentials.Where(c =>
                c.Name.ToLowerInvariant().Contains(searchText) ||
                c.Domain.ToLowerInvariant().Contains(searchText) ||
                c.Username.ToLowerInvariant().Contains(searchText) ||
                c.ModeText.ToLowerInvariant().Contains(searchText)
            ).ToList();
            
            CredentialsListView.ItemsSource = filtered;
        }
    }
    
    /// <summary>
    /// Handles credentials list selection changed
    /// </summary>
    private void CredentialsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CredentialsListView.SelectedItem is Credential credential)
        {
            _viewModel.SelectedCredential = credential;
            StatusTextBlock.Text = $"Đã chọn: {credential.DisplayText}";
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
