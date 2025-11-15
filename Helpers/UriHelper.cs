using System;
using System.Web;

namespace MyFastDownloader.App.Helpers;

/// <summary>
/// Helper class for URI operations
/// </summary>
public static class UriHelper
{
    /// <summary>
    /// Get domain from URL
    /// </summary>
    public static string GetDomain(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host;
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Get file extension from URL
    /// </summary>
    public static string GetFileExtension(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.LocalPath;
            var lastDot = path.LastIndexOf('.');
            
            if (lastDot >= 0)
                return path.Substring(lastDot);
            
            return "";
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// URL encode string
    /// </summary>
    public static string UrlEncode(string value)
    {
        return HttpUtility.UrlEncode(value);
    }

    /// <summary>
    /// URL decode string
    /// </summary>
    public static string UrlDecode(string value)
    {
        return HttpUtility.UrlDecode(value);
    }

    /// <summary>
    /// Combine URLs safely
    /// </summary>
    public static string CombineUrl(string baseUrl, string relativePath)
    {
        try
        {
            var baseUri = new Uri(baseUrl);
            var combinedUri = new Uri(baseUri, relativePath);
            return combinedUri.ToString();
        }
        catch
        {
            return baseUrl;
        }
    }

    /// <summary>
    /// Extract query parameter from URL
    /// </summary>
    public static string? GetQueryParameter(string url, string parameterName)
    {
        try
        {
            var uri = new Uri(url);
            var query = HttpUtility.ParseQueryString(uri.Query);
            return query.Get(parameterName);
        }
        catch
        {
            return null;
        }
    }
}