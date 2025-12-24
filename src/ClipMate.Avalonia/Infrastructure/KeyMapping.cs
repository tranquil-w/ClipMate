using Avalonia.Input;
using ClipMate.Platform.Abstractions.Input;
using AvaloniaKeyModifiers = Avalonia.Input.KeyModifiers;

namespace ClipMate.Avalonia.Infrastructure;

internal static class KeyMapping
{
    public static Key ToAvaloniaKey(VirtualKey key)
    {
        return key switch
        {
            VirtualKey.A => Key.A,
            VirtualKey.B => Key.B,
            VirtualKey.C => Key.C,
            VirtualKey.D => Key.D,
            VirtualKey.E => Key.E,
            VirtualKey.F => Key.F,
            VirtualKey.G => Key.G,
            VirtualKey.H => Key.H,
            VirtualKey.I => Key.I,
            VirtualKey.J => Key.J,
            VirtualKey.K => Key.K,
            VirtualKey.L => Key.L,
            VirtualKey.M => Key.M,
            VirtualKey.N => Key.N,
            VirtualKey.O => Key.O,
            VirtualKey.P => Key.P,
            VirtualKey.Q => Key.Q,
            VirtualKey.R => Key.R,
            VirtualKey.S => Key.S,
            VirtualKey.T => Key.T,
            VirtualKey.U => Key.U,
            VirtualKey.V => Key.V,
            VirtualKey.W => Key.W,
            VirtualKey.X => Key.X,
            VirtualKey.Y => Key.Y,
            VirtualKey.Z => Key.Z,
            VirtualKey.D0 => Key.D0,
            VirtualKey.D1 => Key.D1,
            VirtualKey.D2 => Key.D2,
            VirtualKey.D3 => Key.D3,
            VirtualKey.D4 => Key.D4,
            VirtualKey.D5 => Key.D5,
            VirtualKey.D6 => Key.D6,
            VirtualKey.D7 => Key.D7,
            VirtualKey.D8 => Key.D8,
            VirtualKey.D9 => Key.D9,
            VirtualKey.Enter => Key.Enter,
            VirtualKey.Escape => Key.Escape,
            VirtualKey.Backspace => Key.Back,
            VirtualKey.Tab => Key.Tab,
            VirtualKey.Space => Key.Space,
            VirtualKey.Minus => Key.OemMinus,
            VirtualKey.Equals => Key.OemPlus,
            VirtualKey.LeftBracket => Key.OemOpenBrackets,
            VirtualKey.RightBracket => Key.OemCloseBrackets,
            VirtualKey.Backslash => Key.OemBackslash,
            VirtualKey.Semicolon => Key.OemSemicolon,
            VirtualKey.Quote => Key.OemQuotes,
            VirtualKey.BackQuote => Key.OemTilde,
            VirtualKey.Comma => Key.OemComma,
            VirtualKey.Period => Key.OemPeriod,
            VirtualKey.Slash => Key.OemQuestion,
            VirtualKey.CapsLock => Key.CapsLock,
            VirtualKey.F1 => Key.F1,
            VirtualKey.F2 => Key.F2,
            VirtualKey.F3 => Key.F3,
            VirtualKey.F4 => Key.F4,
            VirtualKey.F5 => Key.F5,
            VirtualKey.F6 => Key.F6,
            VirtualKey.F7 => Key.F7,
            VirtualKey.F8 => Key.F8,
            VirtualKey.F9 => Key.F9,
            VirtualKey.F10 => Key.F10,
            VirtualKey.F11 => Key.F11,
            VirtualKey.F12 => Key.F12,
            VirtualKey.Insert => Key.Insert,
            VirtualKey.Home => Key.Home,
            VirtualKey.PageUp => Key.PageUp,
            VirtualKey.Delete => Key.Delete,
            VirtualKey.End => Key.End,
            VirtualKey.PageDown => Key.PageDown,
            VirtualKey.Right => Key.Right,
            VirtualKey.Left => Key.Left,
            VirtualKey.Down => Key.Down,
            VirtualKey.Up => Key.Up,
            _ => Key.None
        };
    }

    public static AvaloniaKeyModifiers ToAvaloniaModifiers(ClipMate.Platform.Abstractions.Input.KeyModifiers modifiers)
    {
        var result = AvaloniaKeyModifiers.None;
        if (modifiers.HasFlag(ClipMate.Platform.Abstractions.Input.KeyModifiers.Ctrl))
        {
            result |= AvaloniaKeyModifiers.Control;
        }
        if (modifiers.HasFlag(ClipMate.Platform.Abstractions.Input.KeyModifiers.Alt))
        {
            result |= AvaloniaKeyModifiers.Alt;
        }
        if (modifiers.HasFlag(ClipMate.Platform.Abstractions.Input.KeyModifiers.Shift))
        {
            result |= AvaloniaKeyModifiers.Shift;
        }
        if (modifiers.HasFlag(ClipMate.Platform.Abstractions.Input.KeyModifiers.Win))
        {
            result |= AvaloniaKeyModifiers.Meta;
        }

        return result;
    }
}
