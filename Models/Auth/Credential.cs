using System;
using MyFastDownloader.App.Models.Enums;

namespace MyFastDownloader.App.Models.Auth;

/// <summary>
/// Represents authentication credentials for a specific domain/URL
/// </summary>
public class Credential
{
    /// <summary>
    /// Unique identifier for this credential
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Friendly name for this credential (e.g., "GitHub Personal Access Token")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Domain or URL pattern this credential applies to
    /// Examples: "example.com", "https://api.github.com", "*.company.com"
    /// </summary>
    public string Domain { get; set; } = string.Empty;
    
    /// <summary>
    /// Username for authentication
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Encrypted password (never store in plain text)
    /// </summary>
    public string EncryptedPassword { get; set; } = string.Empty;
    
    /// <summary>
    /// Bearer token for OAuth 2.0 authentication (encrypted)
    /// </summary>
    public string? EncryptedToken { get; set; }
    
    /// <summary>
    /// Authentication mode to use
    /// </summary>
    public AuthenticationMode Mode { get; set; } = AuthenticationMode.Basic;
    
    /// <summary>
    /// When this credential was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// When this credential was last used successfully
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
    
    /// <summary>
    /// Number of times this credential has been used
    /// </summary>
    public int UseCount { get; set; } = 0;
    
    /// <summary>
    /// Whether this credential is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Optional notes about this credential
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// Gets display text for UI
    /// </summary>
    public string DisplayText => string.IsNullOrEmpty(Name) 
        ? $"{Username}@{Domain}" 
        : $"{Name} ({Username}@{Domain})";
    
    /// <summary>
    /// Gets authentication mode text
    /// </summary>
    public string ModeText => Mode switch
    {
        AuthenticationMode.None => "Không có",
        AuthenticationMode.Basic => "Basic",
        AuthenticationMode.Digest => "Digest",
        AuthenticationMode.NTLM => "NTLM",
        AuthenticationMode.Bearer => "Bearer Token",
        _ => "Unknown"
    };
}