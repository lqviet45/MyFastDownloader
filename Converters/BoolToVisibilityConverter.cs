using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MyFastDownloader.App.Converters;

/// <summary>
/// Converts boolean to Visibility with optional inversion
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// If true, inverts the logic (true -> Collapsed, false -> Visible)
    /// </summary>
    public bool Invert { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool boolValue)
            return Visibility.Collapsed;

        // Check if parameter is "Invert" or "Inverse"
        if (parameter is string param && 
            (param.Equals("Invert", StringComparison.OrdinalIgnoreCase) ||
             param.Equals("Inverse", StringComparison.OrdinalIgnoreCase)))
        {
            boolValue = !boolValue;
        }
        else if (Invert)
        {
            boolValue = !boolValue;
        }

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Visibility visibility)
            return false;

        bool result = visibility == Visibility.Visible;

        if (parameter is string param && 
            (param.Equals("Invert", StringComparison.OrdinalIgnoreCase) ||
             param.Equals("Inverse", StringComparison.OrdinalIgnoreCase)))
        {
            result = !result;
        }
        else if (Invert)
        {
            result = !result;
        }

        return result;
    }
}