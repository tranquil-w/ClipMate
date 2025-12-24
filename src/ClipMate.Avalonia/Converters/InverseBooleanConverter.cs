using Avalonia.Data.Converters;
using System.Globalization;

namespace ClipMate.Avalonia.Converters;

public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool flag ? !flag : false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool flag ? !flag : false;
    }
}
