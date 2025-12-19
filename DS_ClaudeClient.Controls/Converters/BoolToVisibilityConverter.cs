using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DS_ClaudeClient.Controls.Converters;

/// <summary>
/// Converts boolean values to Visibility.
/// True = Visible, False = Collapsed (or Hidden if parameter is "Hidden").
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValue = value is bool b && b;
        var useHidden = parameter is string s && s.Equals("Hidden", StringComparison.OrdinalIgnoreCase);

        if (boolValue)
        {
            return Visibility.Visible;
        }

        return useHidden ? Visibility.Hidden : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}

/// <summary>
/// Inverted boolean to Visibility converter.
/// False = Visible, True = Collapsed.
/// </summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValue = value is bool b && b;
        return boolValue ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v != Visibility.Visible;
    }
}
