namespace MyFastDownloader.App.Models.Enums;

/// <summary>
/// HTTP authentication modes supported by the application
/// </summary>
public enum AuthenticationMode
{
    /// <summary>
    /// No authentication required
    /// </summary>
    None = 0,
    
    /// <summary>
    /// HTTP Basic Authentication (RFC 7617)
    /// Credentials sent in base64 encoding
    /// </summary>
    Basic = 1,
    
    /// <summary>
    /// HTTP Digest Authentication (RFC 7616)
    /// More secure than Basic, uses challenge-response
    /// </summary>
    Digest = 2,
    
    /// <summary>
    /// Windows NT LAN Manager Authentication
    /// Used for Windows domain authentication
    /// </summary>
    NTLM = 3,
    
    /// <summary>
    /// Bearer token authentication (OAuth 2.0)
    /// </summary>
    Bearer = 4
}