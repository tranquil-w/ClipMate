using System.Diagnostics.CodeAnalysis;

namespace ClipMate.Platform.Abstractions.Input;

public readonly record struct HotkeyDescriptor(VirtualKey Key, KeyModifiers Modifiers)
{
    public string DisplayString => BuildDisplayString(Key, Modifiers);

    public static HotkeyDescriptor Parse(string hotkeyString)
    {
        if (!TryParse(hotkeyString, out var result))
        {
            throw new ArgumentException($"无法解析快捷键字符串: {hotkeyString}", nameof(hotkeyString));
        }

        return result.Value;
    }

    public static bool TryParse(string? hotkeyString, [NotNullWhen(true)] out HotkeyDescriptor? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(hotkeyString))
        {
            return false;
        }

        if (string.Equals(hotkeyString.Trim(), "未设置", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var modifiers = KeyModifiers.None;
        var key = VirtualKey.None;

        var parts = hotkeyString
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var raw in parts)
        {
            var token = raw.Trim();
            if (token.Length == 0)
            {
                continue;
            }

            switch (token.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= KeyModifiers.Ctrl;
                    continue;
                case "alt":
                    modifiers |= KeyModifiers.Alt;
                    continue;
                case "shift":
                    modifiers |= KeyModifiers.Shift;
                    continue;
                case "win":
                case "windows":
                    modifiers |= KeyModifiers.Win;
                    continue;
            }

            if (TryParseKeyToken(token, out var parsedKey))
            {
                key = parsedKey;
            }
        }

        if (key == VirtualKey.None)
        {
            return false;
        }

        result = new HotkeyDescriptor(key, modifiers);
        return true;
    }

    private static bool TryParseKeyToken(string token, out VirtualKey key)
    {
        key = VirtualKey.None;

        if (token.Length == 1)
        {
            var c = token[0];
            if (c is >= 'A' and <= 'Z')
            {
                key = VirtualKey.A + (c - 'A');
                return true;
            }

            if (c is >= 'a' and <= 'z')
            {
                key = VirtualKey.A + (c - 'a');
                return true;
            }

            if (c is >= '0' and <= '9')
            {
                key = c == '0' ? VirtualKey.D0 : VirtualKey.D1 + (c - '1');
                return true;
            }

            key = c switch
            {
                '`' or '~' => VirtualKey.BackQuote,
                '-' => VirtualKey.Minus,
                '=' => VirtualKey.Equals,
                '[' => VirtualKey.LeftBracket,
                ']' => VirtualKey.RightBracket,
                '\\' => VirtualKey.Backslash,
                ';' => VirtualKey.Semicolon,
                '\'' => VirtualKey.Quote,
                ',' => VirtualKey.Comma,
                '.' => VirtualKey.Period,
                '/' => VirtualKey.Slash,
                _ => VirtualKey.None
            };

            return key != VirtualKey.None;
        }

        if (token.Length >= 2 && (token[0] == 'F' || token[0] == 'f') &&
            int.TryParse(token.AsSpan(1), out var f) && f is >= 1 and <= 12)
        {
            key = VirtualKey.F1 + (f - 1);
            return true;
        }

        key = token.ToLowerInvariant() switch
        {
            "enter" => VirtualKey.Enter,
            "return" => VirtualKey.Enter,
            "esc" => VirtualKey.Escape,
            "escape" => VirtualKey.Escape,
            "tab" => VirtualKey.Tab,
            "space" => VirtualKey.Space,
            "backspace" => VirtualKey.Backspace,
            "delete" => VirtualKey.Delete,
            "del" => VirtualKey.Delete,
            "insert" => VirtualKey.Insert,
            "ins" => VirtualKey.Insert,
            "home" => VirtualKey.Home,
            "end" => VirtualKey.End,
            "pageup" => VirtualKey.PageUp,
            "pagedown" => VirtualKey.PageDown,
            "up" => VirtualKey.Up,
            "down" => VirtualKey.Down,
            "left" => VirtualKey.Left,
            "right" => VirtualKey.Right,
            "comma" => VirtualKey.Comma,
            "period" => VirtualKey.Period,
            "dot" => VirtualKey.Period,
            "slash" => VirtualKey.Slash,
            "backslash" => VirtualKey.Backslash,
            "semicolon" => VirtualKey.Semicolon,
            "quote" => VirtualKey.Quote,
            "minus" => VirtualKey.Minus,
            "equal" or "equals" => VirtualKey.Equals,
            "oem3" => VirtualKey.BackQuote,
            _ => VirtualKey.None
        };

        return key != VirtualKey.None;
    }

    private static string BuildDisplayString(VirtualKey key, KeyModifiers modifiers)
    {
        var parts = new List<string>(capacity: 5);

        if (modifiers.HasFlag(KeyModifiers.Ctrl)) parts.Add("Ctrl");
        if (modifiers.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(KeyModifiers.Win)) parts.Add("Win");

        parts.Add(KeyToDisplayToken(key));

        return string.Join(" + ", parts);
    }

    private static string KeyToDisplayToken(VirtualKey key)
    {
        if (key is >= VirtualKey.A and <= VirtualKey.Z)
        {
            var c = (char)('A' + (key - VirtualKey.A));
            return c.ToString();
        }

        if (key is >= VirtualKey.D1 and <= VirtualKey.D9)
        {
            var c = (char)('1' + (key - VirtualKey.D1));
            return c.ToString();
        }

        if (key == VirtualKey.D0)
        {
            return "0";
        }

        if (key is >= VirtualKey.F1 and <= VirtualKey.F12)
        {
            return $"F{1 + (key - VirtualKey.F1)}";
        }

        return key switch
        {
            VirtualKey.Enter => "Enter",
            VirtualKey.Escape => "Escape",
            VirtualKey.Tab => "Tab",
            VirtualKey.Space => "Space",
            VirtualKey.Backspace => "Backspace",
            VirtualKey.Delete => "Delete",
            VirtualKey.Insert => "Insert",
            VirtualKey.Home => "Home",
            VirtualKey.End => "End",
            VirtualKey.PageUp => "PageUp",
            VirtualKey.PageDown => "PageDown",
            VirtualKey.Up => "Up",
            VirtualKey.Down => "Down",
            VirtualKey.Left => "Left",
            VirtualKey.Right => "Right",
            VirtualKey.BackQuote => "`",
            VirtualKey.Minus => "-",
            VirtualKey.Equals => "=",
            VirtualKey.LeftBracket => "[",
            VirtualKey.RightBracket => "]",
            VirtualKey.Backslash => "\\",
            VirtualKey.Semicolon => ";",
            VirtualKey.Quote => "'",
            VirtualKey.Comma => ",",
            VirtualKey.Period => ".",
            VirtualKey.Slash => "/",
            _ => key.ToString()
        };
    }
}

