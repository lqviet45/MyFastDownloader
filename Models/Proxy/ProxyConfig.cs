using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MyFastDownloader.App.Models.Enums;

namespace MyFastDownloader.App.Models.Proxy;

/// <summary>
/// Proxy configuration model
/// </summary>
public class ProxyConfig : INotifyPropertyChanged
{
    private ProxyType _type = ProxyType.None;
    private string _host = "";
    private int _port = 8080;
    private bool _requiresAuth = false;
    private string _username = "";
    private string _encryptedPassword = "";
    private string _bypassList = "";
    private bool _bypassLocalAddresses = true;
    private bool _isActive = true;
    
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Configuration name (optional)
    /// </summary>
    public string Name { get; set; } = "Default Proxy";
    
    /// <summary>
    /// Proxy type
    /// </summary>
    public ProxyType Type
    {
        get => _type;
        set
        {
            _type = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TypeText));
            OnPropertyChanged(nameof(IsConfigurable));
        }
    }
    
    /// <summary>
    /// Proxy host/IP address
    /// </summary>
    public string Host
    {
        get => _host;
        set
        {
            _host = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProxyAddress));
        }
    }
    
    /// <summary>
    /// Proxy port
    /// </summary>
    public int Port
    {
        get => _port;
        set
        {
            _port = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProxyAddress));
        }
    }
    
    /// <summary>
    /// Whether proxy requires authentication
    /// </summary>
    public bool RequiresAuth
    {
        get => _requiresAuth;
        set
        {
            _requiresAuth = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// Username for proxy authentication
    /// </summary>
    public string Username
    {
        get => _username;
        set
        {
            _username = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// Encrypted password for proxy authentication
    /// </summary>
    public string EncryptedPassword
    {
        get => _encryptedPassword;
        set
        {
            _encryptedPassword = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// Bypass list - domains that don't use proxy
    /// Format: domain1.com;*.local;192.168.*
    /// </summary>
    public string BypassList
    {
        get => _bypassList;
        set
        {
            _bypassList = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// Bypass proxy for local addresses
    /// </summary>
    public bool BypassLocalAddresses
    {
        get => _bypassLocalAddresses;
        set
        {
            _bypassLocalAddresses = value;
            OnPropertyChanged();
        }
    }
    
    /// <summary>
    /// Whether this proxy config is active
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set
        {
            _isActive = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
        }
    }
    
    /// <summary>
    /// When this config was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    
    /// <summary>
    /// Last time this config was used
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }
    
    /// <summary>
    /// Number of times this config was used
    /// </summary>
    public int UseCount { get; set; } = 0;
    
    /// <summary>
    /// Optional notes
    /// </summary>
    public string? Notes { get; set; }
    
    // Computed Properties
    
    /// <summary>
    /// Display text for UI
    /// </summary>
    public string DisplayText => string.IsNullOrWhiteSpace(Name) 
        ? $"{TypeText} - {ProxyAddress}" 
        : Name;
    
    /// <summary>
    /// Proxy type as text
    /// </summary>
    public string TypeText => Type switch
    {
        ProxyType.None => "No Proxy",
        ProxyType.System => "System Proxy",
        ProxyType.Http => "HTTP Proxy",
        ProxyType.Https => "HTTPS Proxy",
        ProxyType.Socks4 => "SOCKS4",
        ProxyType.Socks5 => "SOCKS5",
        _ => "Unknown"
    };
    
    /// <summary>
    /// Full proxy address
    /// </summary>
    public string ProxyAddress => Type == ProxyType.None || Type == ProxyType.System
        ? "Auto"
        : string.IsNullOrWhiteSpace(Host)
            ? "Not configured"
            : $"{Host}:{Port}";
    
    /// <summary>
    /// Status text
    /// </summary>
    public string StatusText => IsActive ? "Active" : "Inactive";
    
    /// <summary>
    /// Whether this proxy type can be configured
    /// </summary>
    public bool IsConfigurable => Type != ProxyType.None && Type != ProxyType.System;
    
    /// <summary>
    /// Validates the proxy configuration
    /// </summary>
    public (bool IsValid, string Message) Validate()
    {
        if (Type == ProxyType.None || Type == ProxyType.System)
        {
            return (true, "OK");
        }
        
        if (string.IsNullOrWhiteSpace(Host))
        {
            return (false, "Proxy host is required");
        }
        
        if (Port <= 0 || Port > 65535)
        {
            return (false, "Port must be between 1 and 65535");
        }
        
        if (RequiresAuth)
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                return (false, "Username is required for authentication");
            }
            
            if (string.IsNullOrWhiteSpace(EncryptedPassword))
            {
                return (false, "Password is required for authentication");
            }
        }
        
        return (true, "OK");
    }
    
    /// <summary>
    /// Gets proxy URI string
    /// </summary>
    public string GetProxyUri()
    {
        if (Type == ProxyType.None || Type == ProxyType.System)
        {
            return string.Empty;
        }
        
        var scheme = Type switch
        {
            ProxyType.Http => "http",
            ProxyType.Https => "https",
            ProxyType.Socks4 => "socks4",
            ProxyType.Socks5 => "socks5",
            _ => "http"
        };
        
        if (RequiresAuth && !string.IsNullOrWhiteSpace(Username))
        {
            // Note: Password should be decrypted before use
            return $"{scheme}://{Username}:PASSWORD@{Host}:{Port}";
        }
        
        return $"{scheme}://{Host}:{Port}";
    }
    
    /// <summary>
    /// Checks if URL should bypass proxy
    /// </summary>
    public bool ShouldBypass(string url)
    {
        if (string.IsNullOrWhiteSpace(BypassList))
        {
            return false;
        }
        
        try
        {
            var uri = new Uri(url);
            var host = uri.Host.ToLowerInvariant();
            
            // Check local addresses
            if (BypassLocalAddresses)
            {
                if (host == "localhost" || 
                    host == "127.0.0.1" || 
                    host.StartsWith("192.168.") ||
                    host.StartsWith("10.") ||
                    host.StartsWith("172."))
                {
                    return true;
                }
            }
            
            // Check bypass list
            var patterns = BypassList.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var pattern in patterns)
            {
                var cleanPattern = pattern.Trim().ToLowerInvariant();
                
                if (cleanPattern.StartsWith("*."))
                {
                    // Wildcard pattern
                    var domain = cleanPattern.Substring(2);
                    if (host.EndsWith(domain))
                    {
                        return true;
                    }
                }
                else if (cleanPattern.Contains("*"))
                {
                    // IP range pattern (e.g., 192.168.*)
                    var regex = new System.Text.RegularExpressions.Regex(
                        "^" + System.Text.RegularExpressions.Regex.Escape(cleanPattern).Replace("\\*", ".*") + "$");
                    if (regex.IsMatch(host))
                    {
                        return true;
                    }
                }
                else
                {
                    // Exact match
                    if (host == cleanPattern || host.EndsWith("." + cleanPattern))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}