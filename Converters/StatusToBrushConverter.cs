using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using MyFastDownloader.App.Models.Enums;
using TaskStatus = MyFastDownloader.App.Models.Enums.TaskStatus;

namespace MyFastDownloader.App.Converters;

/// <summary>
/// Converts TaskStatus to appropriate color brush
/// </summary>
[ValueConversion(typeof(TaskStatus), typeof(Brush))]
public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not TaskStatus status)
            return new SolidColorBrush(Colors.Gray);

        return status switch
        {
            TaskStatus.Queued => new SolidColorBrush(Color.FromRgb(75, 85, 99)),       // Gray
            TaskStatus.Downloading => new SolidColorBrush(Color.FromRgb(16, 185, 129)), // Green
            TaskStatus.Paused => new SolidColorBrush(Color.FromRgb(245, 158, 11)),      // Orange
            TaskStatus.Completed => new SolidColorBrush(Color.FromRgb(34, 197, 94)),    // Bright Green
            TaskStatus.Error => new SolidColorBrush(Color.FromRgb(239, 68, 68)),        // Red
            TaskStatus.Canceled => new SolidColorBrush(Color.FromRgb(107, 114, 128)),   // Dark Gray
            _ => new SolidColorBrush(Colors.Gray)
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}