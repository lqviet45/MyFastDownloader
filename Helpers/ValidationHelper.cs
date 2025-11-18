using System;
using System.Text.RegularExpressions;

namespace MyFastDownloader.App.Helpers;

/// <summary>
/// Helper class for validating user inputs
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validates if string is a valid URL
    /// </summary>
    public static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
               && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Validates if string is a valid file path
    /// </summary>
    public static bool IsValidFilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            var fi = new System.IO.FileInfo(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates if port number is valid (1-65535)
    /// </summary>
    public static bool IsValidPort(int port)
    {
        return port >= 1 && port <= 65535;
    }

    /// <summary>
    /// Validates if speed limit value is valid (in KB/s)
    /// </summary>
    public static bool IsValidSpeedLimit(int speedKBps)
    {
        // Minimum 10 KB/s, Maximum 100 MB/s (102400 KB/s)
        return speedKBps >= 10 && speedKBps <= 102400;
    }

    /// <summary>
    /// Validates if number of segments is valid
    /// </summary>
    public static bool IsValidSegmentCount(int segments)
    {
        // Between 1 and 64 segments
        return segments >= 1 && segments <= 64;
    }

    /// <summary>
    /// Validates if string contains only valid filename characters
    /// </summary>
    public static bool IsValidFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;

        // Check for invalid characters
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        return fileName.IndexOfAny(invalidChars) < 0;
    }

    /// <summary>
    /// Sanitizes filename by removing invalid characters
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "download";

        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        
        return string.IsNullOrWhiteSpace(sanitized) ? "download" : sanitized;
    }

    /// <summary>
    /// Validates if timeout value is reasonable (in seconds)
    /// </summary>
    public static bool IsValidTimeout(int timeoutSeconds)
    {
        // Between 5 seconds and 5 minutes
        return timeoutSeconds >= 5 && timeoutSeconds <= 300;
    }

    /// <summary>
    /// Validates retry count
    /// </summary>
    public static bool IsValidRetryCount(int retryCount)
    {
        // Between 0 and 10 retries
        return retryCount >= 0 && retryCount <= 10;
    }
}