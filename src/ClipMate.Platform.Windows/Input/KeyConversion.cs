using ClipMate.Platform.Abstractions.Input;
using System.Windows.Input;

namespace ClipMate.Platform.Windows.Input;

internal static class KeyConversion
{
    internal static bool TryToWpfKey(VirtualKey key, out Key wpfKey)
    {
        wpfKey = Key.None;

        if (key is >= VirtualKey.A and <= VirtualKey.Z)
        {
            wpfKey = Key.A + (key - VirtualKey.A);
            return true;
        }

        if (key is >= VirtualKey.D1 and <= VirtualKey.D9)
        {
            wpfKey = Key.D1 + (key - VirtualKey.D1);
            return true;
        }

        if (key == VirtualKey.D0)
        {
            wpfKey = Key.D0;
            return true;
        }

        if (key is >= VirtualKey.F1 and <= VirtualKey.F12)
        {
            wpfKey = Key.F1 + (key - VirtualKey.F1);
            return true;
        }

        wpfKey = key switch
        {
            VirtualKey.Enter => Key.Enter,
            VirtualKey.Escape => Key.Escape,
            VirtualKey.Tab => Key.Tab,
            VirtualKey.Space => Key.Space,
            VirtualKey.Backspace => Key.Back,
            VirtualKey.Delete => Key.Delete,
            VirtualKey.Insert => Key.Insert,
            VirtualKey.Home => Key.Home,
            VirtualKey.End => Key.End,
            VirtualKey.PageUp => Key.PageUp,
            VirtualKey.PageDown => Key.PageDown,
            VirtualKey.Up => Key.Up,
            VirtualKey.Down => Key.Down,
            VirtualKey.Left => Key.Left,
            VirtualKey.Right => Key.Right,
            VirtualKey.BackQuote => Key.Oem3,
            VirtualKey.Minus => Key.OemMinus,
            VirtualKey.Equals => Key.OemPlus,
            VirtualKey.LeftBracket => Key.OemOpenBrackets,
            VirtualKey.RightBracket => Key.OemCloseBrackets,
            VirtualKey.Backslash => Key.OemBackslash,
            VirtualKey.Semicolon => Key.OemSemicolon,
            VirtualKey.Quote => Key.OemQuotes,
            VirtualKey.Comma => Key.OemComma,
            VirtualKey.Period => Key.OemPeriod,
            VirtualKey.Slash => Key.OemQuestion,
            _ => Key.None
        };

        return wpfKey != Key.None;
    }

    internal static ModifierKeys ToWpfModifiers(KeyModifiers modifiers)
    {
        var result = ModifierKeys.None;
        if (modifiers.HasFlag(KeyModifiers.Ctrl)) result |= ModifierKeys.Control;
        if (modifiers.HasFlag(KeyModifiers.Alt)) result |= ModifierKeys.Alt;
        if (modifiers.HasFlag(KeyModifiers.Shift)) result |= ModifierKeys.Shift;
        if (modifiers.HasFlag(KeyModifiers.Win)) result |= ModifierKeys.Windows;
        return result;
    }
}

