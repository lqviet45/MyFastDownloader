using System;
using System.IO;

namespace MyFastDownloader.App.Helpers;

/// <summary>
/// Helper class for file operations
/// </summary>
public static class FileHelper
{
    /// <summary>
    /// Format bytes to human-readable string
    /// </summary>
    public static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
        int counter = 0;
        double size = bytes;

        while (size >= 1024 && counter < suffixes.Length - 1)
        {
            size /= 1024;
            counter++;
        }

        return $"{size:F1} {suffixes[counter]}";
    }

    /// <summary>
    /// Get safe filename from URL
    /// </summary>
    public static string GetFileNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var fileName = Path.GetFileName(uri.LocalPath);
            
            if (string.IsNullOrWhiteSpace(fileName) || fileName == "/")
            {
                fileName = $"download_{DateTime.Now:yyyyMMddHHmmss}";
            }
            
            return SanitizeFileName(fileName);
        }
        catch
        {
            return $"download_{DateTime.Now:yyyyMMddHHmmss}";
        }
    }

    /// <summary>
    /// Remove invalid characters from filename
    /// </summary>
    public static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in invalidChars)
        {
            fileName = fileName.Replace(c, '_');
        }
        return fileName;
    }

    /// <summary>
    /// Check if path is writable
    /// </summary>
    public static bool IsPathWritable(string path)
    {
        try
        {
            var testFile = Path.Combine(path, $"test_{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get unique filename if file exists
    /// </summary>
    public static string GetUniqueFileName(string filePath)
    {
        if (!File.Exists(filePath))
            return filePath;

        var directory = Path.GetDirectoryName(filePath) ?? "";
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        
        int counter = 1;
        string newFilePath;
        
        do
        {
            newFilePath = Path.Combine(directory, $"{fileNameWithoutExtension} ({counter}){extension}");
            counter++;
        }
        while (File.Exists(newFilePath));

        return newFilePath;
    }
}