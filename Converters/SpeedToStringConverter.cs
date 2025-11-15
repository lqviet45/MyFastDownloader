using System;
using System.Globalization;
using System.Windows.Data;

namespace MyFastDownloader.App.Converters;

/// <summary>
/// Converts speed in bytes/sec to human-readable format (KB/s, MB/s, etc.)
/// </summary>
[ValueConversion(typeof(double), typeof(string))]
public class SpeedToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double speed || speed <= 0)
            return "";

        string[] suffixes = { "B/s", "KB/s", "MB/s", "GB/s" };
        int counter = 0;

        while (speed >= 1024 && counter < suffixes.Length - 1)
        {
            speed /= 1024;
            counter++;
        }

        return $"{speed:F1} {suffixes[counter]}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}