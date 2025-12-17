using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MyFastDownloader.App.Models.Enums;
using MyFastDownloader.App.Models.Proxy;
using MyFastDownloader.App.Services.Proxy;
using MyFastDownloader.App.ViewModels.Base;

namespace MyFastDownloader.App.ViewModels;

/// <summary>
/// ViewModel for proxy configuration management
/// </summary>
public class ProxyViewModel : ViewModelBase
{
    private readonly ProxyManager _proxyManager;
    private string _statusMessage = "Ready";
    private bool _isLoading = false;
    private ProxyConfig? _selectedConfig;
    
    public ObservableCollection<ProxyConfig> Configs { get; set; }
    
    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }
    
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }
    
    public ProxyConfig? SelectedConfig
    {
        get => _selectedConfig;
        set => SetProperty(ref _selectedConfig, value);
    }
    
    public ProxyViewModel(ProxyManager proxyManager)
    {
        _proxyManager = proxyManager;
        Configs = new ObservableCollection<ProxyConfig>();
    }
    
    /// <summary>
    /// Loads all proxy configs
    /// </summary>
    public async Task LoadConfigsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Loading proxy configurations...";
            
            var configs = await _proxyManager.LoadConfigsAsync();
            
            Configs.Clear();
            foreach (var config in configs)
            {
                Configs.Add(config);
            }
            
            StatusMessage = $"Loaded {Configs.Count} proxy configuration(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Error loading configs: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// Adds a new proxy config
    /// </summary>
    public async Task<bool> AddConfigAsync(ProxyConfig config)
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Saving proxy configuration...";
            
            var success = await _proxyManager.SaveConfigAsync(config);
            
            if (success)
            {
                Configs.Add(config);
                StatusMessage = $"✓ Added: {config.DisplayText}";
                return true;
            }
            else
            {
                StatusMessage = "✗ Failed to save proxy configuration";
                return false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Error: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// Updates an existing proxy config
    /// </summary>
    public async Task<bool> UpdateConfigAsync(ProxyConfig config)
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Updating proxy configuration...";
            
            var success = await _proxyManager.SaveConfigAsync(config);
            
            if (success)
            {
                StatusMessage = $"✓ Updated: {config.DisplayText}";
                return true;
            }
            else
            {
                StatusMessage = "✗ Failed to update proxy configuration";
                return false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Error: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// Deletes a proxy config
    /// </summary>
    public async Task<bool> DeleteConfigAsync(ProxyConfig config)
    {
        try
        {
            var result = MessageBox.Show(
                $"Bạn có chắc muốn xóa proxy '{config.DisplayText}'?",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result != MessageBoxResult.Yes)
                return false;
            
            IsLoading = true;
            StatusMessage = "Deleting proxy configuration...";
            
            var success = await _proxyManager.DeleteConfigAsync(config.Id);
            
            if (success)
            {
                Configs.Remove(config);
                StatusMessage = $"✓ Deleted: {config.DisplayText}";
                return true;
            }
            else
            {
                StatusMessage = "✗ Failed to delete proxy configuration";
                return false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Error: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// Toggles active state of a proxy config
    /// </summary>
    public async Task ToggleConfigAsync(ProxyConfig config)
    {
        try
        {
            IsLoading = true;
            
            var success = await _proxyManager.ToggleConfigAsync(config.Id);
            
            if (success)
            {
                // Refresh all configs to update IsActive state
                await LoadConfigsAsync();
                
                StatusMessage = config.IsActive 
                    ? $"✓ Activated: {config.DisplayText}" 
                    : $"✓ Deactivated: {config.DisplayText}";
            }
            else
            {
                StatusMessage = "✗ Failed to toggle proxy";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"✗ Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// Tests proxy connection
    /// </summary>
    public async Task<(bool Success, string Message)> TestConfigAsync(ProxyConfig config, string testUrl)
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Testing proxy connection...";
            
            var (success, message) = await _proxyManager.TestProxyAsync(config, testUrl);
            
            StatusMessage = message;
            return (success, message);
        }
        catch (Exception ex)
        {
            var errorMsg = $"✗ Test failed: {ex.Message}";
            StatusMessage = errorMsg;
            return (false, errorMsg);
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// Gets available proxy types
    /// </summary>
    public ProxyType[] GetProxyTypes()
    {
        return new[]
        {
            ProxyType.None,
            ProxyType.System,
            ProxyType.Http,
            ProxyType.Https,
            ProxyType.Socks4,
            ProxyType.Socks5
        };
    }
    
    /// <summary>
    /// Gets proxy type display name
    /// </summary>
    public string GetProxyTypeName(ProxyType type)
    {
        return type switch
        {
            ProxyType.None => "No Proxy (Direct)",
            ProxyType.System => "System Proxy (Auto)",
            ProxyType.Http => "HTTP Proxy",
            ProxyType.Https => "HTTPS Proxy",
            ProxyType.Socks4 => "SOCKS4",
            ProxyType.Socks5 => "SOCKS5",
            _ => "Unknown"
        };
    }
    
    /// <summary>
    /// Gets active proxy config
    /// </summary>
    public ProxyConfig? GetActiveConfig()
    {
        return Configs.FirstOrDefault(c => c.IsActive);
    }
}