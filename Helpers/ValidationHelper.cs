using System;
using System.Text.RegularExpressions;

namespace MyFastDownloader.App.Helpers;

/// <summary>
/// Helper class for input validation
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validate if string is a valid URL
    /// </summary>
    public static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Validate if URL is downloadable (http/https only)
    /// </summary>
    public static bool IsDownloadableUrl(string url)
    {
        if (!IsValidUrl(url))
            return false;

        var uri = new Uri(url);
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    /// <summary>
    /// Validate segment count
    /// </summary>
    public static bool IsValidSegmentCount(int count)
    {
        return count >= 1 && count <= 16;
    }

    /// <summary>
    /// Validate speed limit in bytes per second
    /// </summary>
    public static bool IsValidSpeedLimit(long bytesPerSecond)
    {
        return bytesPerSecond >= 0 && bytesPerSecond <= 1024L * 1024 * 1024 * 10; // Max 10 GB/s
    }

    /// <summary>
    /// Validate email address
    /// </summary>
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return regex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validate port number
    /// </summary>
    public static bool IsValidPort(int port)
    {
        return port >= 1 && port <= 65535;
    }

    /// <summary>
    /// Validate concurrent downloads count
    /// </summary>
    public static bool IsValidConcurrentCount(int count)
    {
        return count >= 1 && count <= 10;
    }
}