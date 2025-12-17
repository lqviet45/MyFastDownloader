using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MyFastDownloader.App.Models.Enums;
using MyFastDownloader.App.Models.Proxy;

namespace MyFastDownloader.App.Services.Proxy;

/// <summary>
/// Manages proxy configurations with encryption and validation
/// </summary>
public class ProxyManager
{
    private readonly string _configFilePath;
    private readonly byte[] _entropy = Encoding.UTF8.GetBytes("MyFastDownloader_Proxy_Entropy_2025");
    private List<ProxyConfig> _configs = new();
    
    public ProxyManager()
    {
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyFastDownloader");
        
        Directory.CreateDirectory(appDataFolder);
        _configFilePath = Path.Combine(appDataFolder, "proxy_configs.dat");
    }
    
    /// <summary>
    /// Encrypts password using Windows DPAPI
    /// </summary>
    public string EncryptPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return string.Empty;

        try
        {
            var data = Encoding.UTF8.GetBytes(password);
            var encrypted = ProtectedData.Protect(data, _entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Failed to encrypt password : {e.Message}", e);
        }
    }
    
    /// <summary>
    /// Decrypts password using Windows DPAPI
    /// </summary>
    public string DecryptPassword(string encryptedPassword)
    {
        if (string.IsNullOrEmpty(encryptedPassword))
            return string.Empty;

        try
        {
            var data = Convert.FromBase64String(encryptedPassword);
            var decrypted = ProtectedData.Unprotect(data, _entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Failed to decrypt password : {e.Message}", e);
        }
    }

    /// <summary>
    /// Loads proxy configs from encrypted storage
    /// </summary>
    public async Task<List<ProxyConfig>> LoadConfigsAsync()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                _configs = new List<ProxyConfig>();
                return _configs;
            }
            
            var encryptedData = await File.ReadAllBytesAsync(_configFilePath);
            var jsonData = ProtectedData.Unprotect(encryptedData, _entropy, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(jsonData);
            
            _configs = JsonSerializer.Deserialize<List<ProxyConfig>>(json) ?? new List<ProxyConfig>();
            return _configs;
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load proxy configs: {e.Message}");
            _configs = new List<ProxyConfig>();
            return _configs;
        }
    }

    /// <summary>
    /// Saves proxy configs to encrypted storage
    /// </summary>
    public async Task SaveConfigsAsync(List<ProxyConfig> configs)
    {
        try
        {
            _configs = configs;
            
            var json = JsonSerializer.Serialize(configs, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            var jsonData = Encoding.UTF8.GetBytes(json);
            var encryptedData = ProtectedData.Protect(jsonData, _entropy, DataProtectionScope.CurrentUser);
            
            await File.WriteAllBytesAsync(_configFilePath, encryptedData);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save proxy configs: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Saves a single proxy config
    /// </summary>
    public async Task<bool> SaveConfigAsync(ProxyConfig config)
    {
        try
        {
            var (isValid, message) = config.Validate();
            if (!isValid)
            {
                throw new InvalidOperationException(message);
            }
            
            var existingIndex = _configs.FindIndex(c => c.Id == config.Id);
            if (existingIndex >= 0)
            {
                _configs[existingIndex] = config;
            }
            else
            {
                _configs.Add(config);
            }
            
            await SaveConfigsAsync(_configs);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save proxy config: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Deletes a proxy config
    /// </summary>
    public async Task<bool> DeleteConfigAsync(Guid id)
    {
        try
        {
            _configs.RemoveAll(c => c.Id == id);
            await SaveConfigsAsync(_configs);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to delete proxy config: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Gets active proxy config
    /// </summary>
    public ProxyConfig? GetActiveConfig()
    {
        return _configs.FirstOrDefault(c => c.IsActive);
    }
    
    /// <summary>
    /// Gets all proxy configs
    /// </summary>
    public List<ProxyConfig> GetAllConfigs()
    {
        return _configs;
    }
    
    /// <summary>
    /// Toggles active state of a config
    /// </summary>
    public async Task<bool> ToggleConfigAsync(Guid id)
    {
        try
        {
            var config = _configs.FirstOrDefault(c => c.Id == id);
            if (config == null)
                return false;
            
            // Deactivate all others if activating this one
            if (!config.IsActive)
            {
                foreach (var c in _configs.Where(c => c.Id != id))
                {
                    c.IsActive = false;
                }
            }
            
            config.IsActive = !config.IsActive;
            await SaveConfigsAsync(_configs);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to toggle proxy config: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Updates usage statistics
    /// </summary>
    public async Task UpdateUsageAsync(Guid id)
    {
        try
        {
            var config = _configs.FirstOrDefault(c => c.Id == id);
            if (config != null)
            {
                config.UseCount++;
                config.LastUsedAt = DateTimeOffset.Now;
                await SaveConfigsAsync(_configs);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to update proxy usage: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Tests proxy connection
    /// </summary>
    public async Task<(bool Success, string Message)> TestProxyAsync(ProxyConfig config, string testUrl = "https://www.google.com")
    {
        try
        {
            var (isValid, validationMessage) = config.Validate();
            if (!isValid)
            {
                return (false, $"⚠ Invalid configuration: {validationMessage}");
            }
            
            if (config.Type == ProxyType.None)
            {
                return (true, "✓ Direct connection (no proxy)");
            }
            
            if (config.Type == ProxyType.System)
            {
                return await TestSystemProxyAsync(testUrl);
            }
            
            // Test custom proxy
            var handler = new HttpClientHandler();
            
            if (config.Type == ProxyType.Http || config.Type == ProxyType.Https)
            {
                var proxyUri = new Uri($"http://{config.Host}:{config.Port}");
                handler.Proxy = new WebProxy(proxyUri);
                handler.UseProxy = true;
                
                if (config.RequiresAuth && !string.IsNullOrWhiteSpace(config.Username))
                {
                    var password = DecryptPassword(config.EncryptedPassword);
                    handler.Proxy.Credentials = new NetworkCredential(config.Username, password);
                }
            }
            else if (config.Type == ProxyType.Socks4 || config.Type == ProxyType.Socks5)
            {
                // SOCKS proxies require special handling
                // For now, return not implemented
                return (false, "⚠ SOCKS proxy testing not yet implemented. Will work in downloads.");
            }
            
            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(15);

            var response = await client.GetAsync(testUrl);
            
            if (response.IsSuccessStatusCode)
            {
                return (true, $"✓ Proxy connection successful! ({response.StatusCode})");
            }
            else
            {
                return (false, $"✗ Proxy returned error: {response.StatusCode}");
            }
        }
        catch (HttpRequestException ex)
        {
            return (false, $"✗ Connection failed: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return (false, "✗ Connection timeout (15 seconds)");
        }
        catch (Exception ex)
        {
            return (false, $"✗ Test failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Tests system proxy
    /// </summary>
    private async Task<(bool Success, string Message)> TestSystemProxyAsync(string testUrl)
    {
        try
        {
            var handler = new HttpClientHandler
            {
                UseProxy = true,
                Proxy = WebRequest.GetSystemWebProxy()
            };
            
            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(15);
            var response = await client.GetAsync(testUrl);
            
            if (response.IsSuccessStatusCode)
            {
                return (true, $"✓ System proxy works! ({response.StatusCode})");
            }
            else
            {
                return (false, $"✗ System proxy error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"✗ System proxy test failed: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Creates HttpClientHandler configured with proxy
    /// </summary>
    public HttpClientHandler CreateProxyHandler(ProxyConfig? config = null)
    {
        config ??= GetActiveConfig();
        
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.All,
            MaxConnectionsPerServer = 32
        };
        
        if (config == null || config.Type == ProxyType.None)
        {
            handler.UseProxy = false;
            return handler;
        }
        
        if (config.Type == ProxyType.System)
        {
            handler.UseProxy = true;
            handler.Proxy = WebRequest.GetSystemWebProxy();
            handler.Proxy.Credentials = CredentialCache.DefaultCredentials;
            return handler;
        }
        
        // Custom proxy
        var proxyUri = new Uri($"http://{config.Host}:{config.Port}");
        handler.Proxy = new WebProxy(proxyUri);
        handler.UseProxy = true;
        
        if (config.RequiresAuth && !string.IsNullOrWhiteSpace(config.Username))
        {
            var password = DecryptPassword(config.EncryptedPassword);
            handler.Proxy.Credentials = new NetworkCredential(config.Username, password);
        }
        
        // Set bypass list
        if (!string.IsNullOrWhiteSpace(config.BypassList))
        {
            var bypassList = config.BypassList.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();
            
            ((WebProxy)handler.Proxy).BypassList = bypassList;
        }
        
        ((WebProxy)handler.Proxy).BypassProxyOnLocal = config.BypassLocalAddresses;
        
        return handler;
    }
    
    /// <summary>
    /// Checks if URL should bypass proxy
    /// </summary>
    public bool ShouldBypass(string url)
    {
        var config = GetActiveConfig();
        return config?.ShouldBypass(url) ?? false;
    }
}