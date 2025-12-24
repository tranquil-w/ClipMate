using Avalonia.Data;
using Avalonia.Data.Converters;
using ClipMate.Presentation.Clipboard;
using System.Globalization;

namespace ClipMate.Avalonia.Converters;

public sealed class SelectedItemFavoriteConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isFavorite = value is IClipboardContent item && item.IsFavorite;
        if (parameter is string text &&
            text.Equals("Invert", StringComparison.OrdinalIgnoreCase))
        {
            return !isFavorite;
        }

        return isFavorite;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
