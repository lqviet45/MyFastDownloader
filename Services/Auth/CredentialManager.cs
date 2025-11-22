using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MyFastDownloader.App.Models.Auth;
using MyFastDownloader.App.Models.Enums;

namespace MyFastDownloader.App.Services.Auth;

/// <summary>
/// Manages authentication credentials with secure storage using Windows DPAPI
/// </summary>
public class CredentialManager
{
    private readonly string _credentialsFilePath;
    private List<Credential> _credentials = new();
    private readonly object _lock = new object();
    
    // Entropy for additional encryption security
    private static readonly byte[] _entropy = Encoding.UTF8.GetBytes("MyFastDownloader_V1_2025");
    
    public CredentialManager()
    {
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyFastDownloader");
        
        Directory.CreateDirectory(appDataFolder);
        _credentialsFilePath = Path.Combine(appDataFolder, "credentials.dat");
    }
    
    /// <summary>
    /// Loads all credentials from encrypted storage
    /// </summary>
    public async Task<List<Credential>> LoadCredentialsAsync()
    {
        lock (_lock)
        {
            if (_credentials.Any())
                return new List<Credential>(_credentials);
        }
        
        try
        {
            if (File.Exists(_credentialsFilePath))
            {
                var encryptedData = await File.ReadAllBytesAsync(_credentialsFilePath);
                var decryptedData = ProtectedData.Unprotect(encryptedData, _entropy, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(decryptedData);
                
                var credentials = JsonSerializer.Deserialize<List<Credential>>(json) ?? new List<Credential>();
                
                lock (_lock)
                {
                    _credentials = credentials;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load credentials: {ex.Message}");
            _credentials = new List<Credential>();
        }
        
        return new List<Credential>(_credentials);
    }
    
    /// <summary>
    /// Saves all credentials to encrypted storage
    /// </summary>
    public async Task SaveCredentialsAsync()
    {
        try
        {
            List<Credential> credentialsToSave;
            lock (_lock)
            {
                credentialsToSave = new List<Credential>(_credentials);
            }
            
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(credentialsToSave, options);
            var dataToEncrypt = Encoding.UTF8.GetBytes(json);
            var encryptedData = ProtectedData.Protect(dataToEncrypt, _entropy, DataProtectionScope.CurrentUser);
            
            await File.WriteAllBytesAsync(_credentialsFilePath, encryptedData);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save credentials: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Adds or updates a credential
    /// </summary>
    public async Task<Credential> SaveCredentialAsync(Credential credential)
    {
        if (credential == null)
            throw new ArgumentNullException(nameof(credential));
        
        lock (_lock)
        {
            var existing = _credentials.FirstOrDefault(c => c.Id == credential.Id);
            if (existing != null)
            {
                _credentials.Remove(existing);
            }
            
            _credentials.Add(credential);
        }
        
        await SaveCredentialsAsync();
        return credential;
    }
    
    /// <summary>
    /// Deletes a credential
    /// </summary>
    public async Task<bool> DeleteCredentialAsync(Guid credentialId)
    {
        bool removed;
        lock (_lock)
        {
            removed = _credentials.RemoveAll(c => c.Id == credentialId) > 0;
        }
        
        if (removed)
        {
            await SaveCredentialsAsync();
        }
        
        return removed;
    }
    
    /// <summary>
    /// Gets credential for a specific URL
    /// </summary>
    public Credential? GetCredentialForUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;
        
        try
        {
            var uri = new Uri(url);
            var host = uri.Host.ToLowerInvariant();
            
            lock (_lock)
            {
                // First, try exact domain match
                var exactMatch = _credentials
                    .Where(c => c.IsActive)
                    .FirstOrDefault(c => c.Domain.ToLowerInvariant() == host);
                
                if (exactMatch != null)
                    return exactMatch;
                
                // Then try wildcard match (*.example.com)
                var wildcardMatch = _credentials
                    .Where(c => c.IsActive && c.Domain.StartsWith("*."))
                    .FirstOrDefault(c =>
                    {
                        var domain = c.Domain.Substring(2).ToLowerInvariant();
                        return host.EndsWith(domain);
                    });
                
                if (wildcardMatch != null)
                    return wildcardMatch;
                
                // Finally, try subdomain match
                var subdomainMatch = _credentials
                    .Where(c => c.IsActive)
                    .FirstOrDefault(c => host.Contains(c.Domain.ToLowerInvariant()));
                
                return subdomainMatch;
            }
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Encrypts a password using Windows DPAPI
    /// </summary>
    public string EncryptPassword(string plainPassword)
    {
        if (string.IsNullOrEmpty(plainPassword))
            return string.Empty;
        
        try
        {
            var dataToEncrypt = Encoding.UTF8.GetBytes(plainPassword);
            var encryptedData = ProtectedData.Protect(dataToEncrypt, _entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to encrypt password: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Decrypts a password using Windows DPAPI
    /// </summary>
    public string DecryptPassword(string encryptedPassword)
    {
        if (string.IsNullOrEmpty(encryptedPassword))
            return string.Empty;
        
        try
        {
            var encryptedData = Convert.FromBase64String(encryptedPassword);
            var decryptedData = ProtectedData.Unprotect(encryptedData, _entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedData);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to decrypt password: {ex.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// Tests if a credential works by making a test request
    /// </summary>
    public async Task<(bool Success, string Message)> TestCredentialAsync(Credential credential, string testUrl)
    {
        try
        {
            using var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 3
            };
            
            using var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            
            // Add authentication header based on mode
            if (credential.Mode == AuthenticationMode.Basic)
            {
                var password = DecryptPassword(credential.EncryptedPassword);
                var authValue = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{credential.Username}:{password}"));
                client.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Basic", authValue);
            }
            else if (credential.Mode == AuthenticationMode.Bearer)
            {
                var token = credential.EncryptedToken != null 
                    ? DecryptPassword(credential.EncryptedToken) 
                    : string.Empty;
                client.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);
            }
            
            var response = await client.GetAsync(testUrl);
            
            if (response.IsSuccessStatusCode)
            {
                return (true, "✓ Kết nối thành công!");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return (false, "✗ Xác thực thất bại - Kiểm tra username/password");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return (false, "✗ Không có quyền truy cập");
            }
            else
            {
                return (false, $"✗ Lỗi: HTTP {(int)response.StatusCode}");
            }
        }
        catch (TaskCanceledException)
        {
            return (false, "✗ Timeout - Server không phản hồi");
        }
        catch (HttpRequestException ex)
        {
            return (false, $"✗ Lỗi kết nối: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, $"✗ Lỗi: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Updates credential usage statistics
    /// </summary>
    public async Task UpdateCredentialUsageAsync(Guid credentialId)
    {
        lock (_lock)
        {
            var credential = _credentials.FirstOrDefault(c => c.Id == credentialId);
            if (credential != null)
            {
                credential.LastUsedAt = DateTime.Now;
                credential.UseCount++;
            }
        }
        
        await SaveCredentialsAsync();
    }
    
    /// <summary>
    /// Gets all active credentials
    /// </summary>
    public List<Credential> GetActiveCredentials()
    {
        lock (_lock)
        {
            return _credentials.Where(c => c.IsActive).ToList();
        }
    }
    
    /// <summary>
    /// Toggles credential active status
    /// </summary>
    public async Task<bool> ToggleCredentialAsync(Guid credentialId)
    {
        bool changed = false;
        lock (_lock)
        {
            var credential = _credentials.FirstOrDefault(c => c.Id == credentialId);
            if (credential != null)
            {
                credential.IsActive = !credential.IsActive;
                changed = true;
            }
        }
        
        if (changed)
        {
            await SaveCredentialsAsync();
        }
        
        return changed;
    }
}