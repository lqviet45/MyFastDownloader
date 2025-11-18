using System;
using System.Globalization;
using System.Windows.Data;

namespace MyFastDownloader.App.Converters;

/// <summary>
/// Converts download speed (bytes/sec) to human-readable string (KB/s, MB/s)
/// </summary>
public class SpeedToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double speedBytesPerSec || speedBytesPerSec <= 0)
            return "0 KB/s";

        return FormatSpeed(speedBytesPerSec);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond <= 0)
            return "0 KB/s";

        if (bytesPerSecond < 1024)
            return $"{bytesPerSecond:0.##} B/s";

        if (bytesPerSecond < 1024 * 1024)
            return $"{bytesPerSecond / 1024:0.##} KB/s";

        if (bytesPerSecond < 1024 * 1024 * 1024)
            return $"{bytesPerSecond / (1024 * 1024):0.##} MB/s";

        return $"{bytesPerSecond / (1024 * 1024 * 1024):0.##} GB/s";
    }
}