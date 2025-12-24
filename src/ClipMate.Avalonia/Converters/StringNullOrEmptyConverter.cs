using Avalonia.Data;
using Avalonia.Data.Converters;
using System.Globalization;

namespace ClipMate.Avalonia.Converters;

public sealed class StringNullOrEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not string text || string.IsNullOrEmpty(text);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
