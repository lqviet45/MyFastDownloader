using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MyFastDownloader.App.Models.Auth;
using MyFastDownloader.App.Models.Enums;
using MyFastDownloader.App.Services.Auth;
using MyFastDownloader.App.ViewModels.Base;

namespace MyFastDownloader.App.ViewModels;

/// <summary>
/// ViewModel for managing authentication credentials
/// </summary>
public class CredentialViewModel : ViewModelBase
{
    private readonly CredentialManager _credentialManager;
    
    public ObservableCollection<Credential> Credentials { get; set; }
    
    private Credential? _selectedCredential;
    public Credential? SelectedCredential
    {
        get => _selectedCredential;
        set => SetProperty(ref _selectedCredential, value);
    }
    
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    
    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    public CredentialViewModel(CredentialManager credentialManager)
    {
        _credentialManager = credentialManager;
        Credentials = new ObservableCollection<Credential>();
    }
    
    /// <summary>
    /// Loads all credentials
    /// </summary>
    public async Task LoadCredentialsAsync()
    {
        IsLoading = true;
        StatusMessage = "Đang tải credentials...";
        
        try
        {
            var credentials = await _credentialManager.LoadCredentialsAsync();
            
            Credentials.Clear();
            foreach (var cred in credentials.OrderByDescending(c => c.LastUsedAt ?? c.CreatedAt))
            {
                Credentials.Add(cred);
            }
            
            StatusMessage = $"Đã tải {Credentials.Count} credentials";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
            MessageBox.Show($"Không thể tải credentials: {ex.Message}", 
                          "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// Adds a new credential
    /// </summary>
    public async Task<bool> AddCredentialAsync(Credential credential)
    {
        try
        {
            var saved = await _credentialManager.SaveCredentialAsync(credential);
            Credentials.Insert(0, saved);
            SelectedCredential = saved;
            StatusMessage = $"✓ Đã thêm credential: {saved.DisplayText}";
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Lỗi: {ex.Message}";
            MessageBox.Show($"Không thể lưu credential: {ex.Message}", 
                          "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }
    
    /// <summary>
    /// Updates an existing credential
    /// </summary>
    public async Task<bool> UpdateCredentialAsync(Credential credential)
    {
        try
        {
            await _credentialManager.SaveCredentialAsync(credential);
            
            // Refresh list
            var index = Credentials.IndexOf(Credentials.FirstOrDefault(c => c.Id == credential.Id));
            if (index >= 0)
            {
                Credentials[index] = credential;
            }
            
            StatusMessage = $"✓ Đã cập nhật credential: {credential.DisplayText}";
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Lỗi: {ex.Message}";
            MessageBox.Show($"Không thể cập nhật credential: {ex.Message}", 
                          "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }
    
    /// <summary>
    /// Deletes a credential
    /// </summary>
    public async Task<bool> DeleteCredentialAsync(Credential credential)
    {
        var result = MessageBox.Show(
            $"Bạn có chắc muốn xóa credential '{credential.DisplayText}'?",
            "Xác nhận xóa",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result != MessageBoxResult.Yes)
            return false;
        
        try
        {
            await _credentialManager.DeleteCredentialAsync(credential.Id);
            Credentials.Remove(credential);
            StatusMessage = $"✓ Đã xóa credential: {credential.DisplayText}";
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Lỗi: {ex.Message}";
            MessageBox.Show($"Không thể xóa credential: {ex.Message}", 
                          "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }
    
    /// <summary>
    /// Tests a credential
    /// </summary>
    public async Task TestCredentialAsync(Credential credential, string testUrl)
    {
        IsLoading = true;
        StatusMessage = "Đang kiểm tra kết nối...";
        
        try
        {
            var (success, message) = await _credentialManager.TestCredentialAsync(credential, testUrl);
            StatusMessage = message;
            
            if (success)
            {
                MessageBox.Show(message, "Thành công", 
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(message, "Thất bại", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Lỗi: {ex.Message}";
            MessageBox.Show($"Lỗi khi kiểm tra: {ex.Message}", 
                          "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// Toggles credential active status
    /// </summary>
    public async Task ToggleCredentialAsync(Credential credential)
    {
        try
        {
            await _credentialManager.ToggleCredentialAsync(credential.Id);
            
            // Refresh the item in collection
            var index = Credentials.IndexOf(credential);
            if (index >= 0)
            {
                Credentials[index] = credential;
                OnPropertyChanged(nameof(Credentials));
            }
            
            var status = credential.IsActive ? "kích hoạt" : "vô hiệu hóa";
            StatusMessage = $"✓ Đã {status} credential: {credential.DisplayText}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Lỗi: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Gets available authentication modes
    /// </summary>
    public AuthenticationMode[] GetAuthenticationModes()
    {
        return new[]
        {
            AuthenticationMode.Basic,
            AuthenticationMode.Digest,
            AuthenticationMode.NTLM,
            AuthenticationMode.Bearer
        };
    }
}