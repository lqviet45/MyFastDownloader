namespace MyFastDownloader.App.Models.Enums;

/// <summary>
/// Proxy protocol types
/// </summary>
public enum ProxyType
{
    /// <summary>
    /// No proxy - direct connection
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Use system proxy settings from Windows
    /// </summary>
    System = 1,
    
    /// <summary>
    /// HTTP proxy (CONNECT method for HTTPS)
    /// Most common, works with HTTP and HTTPS
    /// </summary>
    Http = 2,
    
    /// <summary>
    /// HTTPS proxy (encrypted tunnel)
    /// Secure proxy connection
    /// </summary>
    Https = 3,
    
    /// <summary>
    /// SOCKS4 proxy
    /// TCP connections only, no authentication
    /// </summary>
    Socks4 = 4,
    
    /// <summary>
    /// SOCKS5 proxy
    /// TCP/UDP, supports authentication
    /// Most versatile proxy protocol
    /// </summary>
    Socks5 = 5
}