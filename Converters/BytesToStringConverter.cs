using System;
using System.Globalization;
using System.Windows.Data;

namespace MyFastDownloader.App.Converters;

/// <summary>
/// Converts bytes to human-readable format (KB, MB, GB, TB)
/// </summary>
/// <example>
/// 1024 bytes → "1 KB"
/// 1048576 bytes → "1 MB"
/// </example>
public class BytesToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not long bytes)
            return "0 B";

        return FormatBytes(bytes);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}