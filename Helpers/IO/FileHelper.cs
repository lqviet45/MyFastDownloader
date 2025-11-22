using System;
using System.IO;

namespace MyFastDownloader.App.Helpers.IO;

/// <summary>
/// Helper class for file operations
/// </summary>
public static class FileHelper
{
    /// <summary>
    /// Ensures directory exists, creates if not
    /// </summary>
    public static void EnsureDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>
    /// Gets a unique filename if file already exists by appending (1), (2), etc.
    /// </summary>
    public static string GetUniqueFilePath(string filePath)
    {
        if (!File.Exists(filePath))
            return filePath;

        var directory = Path.GetDirectoryName(filePath) ?? "";
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        var counter = 1;

        string newPath;
        do
        {
            var newFileName = $"{fileNameWithoutExt} ({counter}){extension}";
            newPath = Path.Combine(directory, newFileName);
            counter++;
        } while (File.Exists(newPath));

        return newPath;
    }

    /// <summary>
    /// Safely deletes a file if it exists
    /// </summary>
    public static bool TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets file size in bytes, returns 0 if file doesn't exist
    /// </summary>
    public static long GetFileSize(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                return fileInfo.Length;
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Checks if file path is valid
    /// </summary>
    public static bool IsValidFilePath(string path)
    {
        try
        {
            var fi = new FileInfo(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets default downloads folder
    /// </summary>
    public static string GetDefaultDownloadsFolder()
    {
        var downloadsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads"
        );

        if (!Directory.Exists(downloadsPath))
        {
            Directory.CreateDirectory(downloadsPath);
        }

        return downloadsPath;
    }
}