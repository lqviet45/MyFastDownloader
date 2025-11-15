using System;
using System.Globalization;
using System.Windows.Data;

namespace MyFastDownloader.App.Converters;

/// <summary>
/// Converts bytes to human-readable string format (KB, MB, GB, etc.)
/// </summary>
[ValueConversion(typeof(long), typeof(string))]
public class BytesToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not long bytes && value is not double)
            return "0 B";

        double size = value is long l ? l : (double)value;
        string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
        int counter = 0;

        while (size >= 1024 && counter < suffixes.Length - 1)
        {
            size /= 1024;
            counter++;
        }

        return $"{size:F1} {suffixes[counter]}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}