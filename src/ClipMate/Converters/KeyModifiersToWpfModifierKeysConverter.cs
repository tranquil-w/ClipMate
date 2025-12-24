using ClipMate.Platform.Abstractions.Input;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace ClipMate.Converters;

public sealed class KeyModifiersToWpfModifierKeysConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not KeyModifiers modifiers)
        {
            return ModifierKeys.None;
        }

        var result = ModifierKeys.None;
        if (modifiers.HasFlag(KeyModifiers.Ctrl))
        {
            result |= ModifierKeys.Control;
        }
        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            result |= ModifierKeys.Alt;
        }
        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            result |= ModifierKeys.Shift;
        }
        if (modifiers.HasFlag(KeyModifiers.Win))
        {
            result |= ModifierKeys.Windows;
        }

        return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return KeyModifiers.None;
    }
}
