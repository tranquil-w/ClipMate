using ClipMate.Platform.Abstractions.Input;
using ClipMate.Services;
using System.Reflection;

namespace ClipMate.Tests.Services;

/// <summary>
/// MainWindowOverlayService 的单元测试：仅覆盖字符转换逻辑（TryGetPrintableText）。
/// </summary>
public class MainWindowOverlayServiceTests
{
    [Theory]
    [InlineData(VirtualKey.A, "a")]
    [InlineData(VirtualKey.B, "b")]
    [InlineData(VirtualKey.Z, "z")]
    public void TryGetPrintableText_Letters_ShouldReturnLowercaseChar(VirtualKey key, string expected)
    {
        var result = InvokeTryGetPrintableText(key, KeyModifiers.None, out var text);

        Assert.True(result);
        Assert.Equal(expected, text);
    }

    [Theory]
    [InlineData(VirtualKey.A, "A")]
    [InlineData(VirtualKey.B, "B")]
    [InlineData(VirtualKey.Z, "Z")]
    public void TryGetPrintableText_LettersWithShift_ShouldReturnUppercaseChar(VirtualKey key, string expected)
    {
        var result = InvokeTryGetPrintableText(key, KeyModifiers.Shift, out var text);

        Assert.True(result);
        Assert.Equal(expected, text);
    }

    [Theory]
    [InlineData(VirtualKey.D0, "0")]
    [InlineData(VirtualKey.D5, "5")]
    [InlineData(VirtualKey.D9, "9")]
    public void TryGetPrintableText_Numbers_ShouldReturnCorrectChar(VirtualKey key, string expected)
    {
        var result = InvokeTryGetPrintableText(key, KeyModifiers.None, out var text);

        Assert.True(result);
        Assert.Equal(expected, text);
    }

    [Theory]
    [InlineData(VirtualKey.Space, " ")]
    [InlineData(VirtualKey.Comma, ",")]
    [InlineData(VirtualKey.Period, ".")]
    [InlineData(VirtualKey.Slash, "/")]
    [InlineData(VirtualKey.Semicolon, ";")]
    [InlineData(VirtualKey.Quote, "'")]
    [InlineData(VirtualKey.Minus, "-")]
    [InlineData(VirtualKey.Equals, "=")]
    [InlineData(VirtualKey.LeftBracket, "[")]
    [InlineData(VirtualKey.RightBracket, "]")]
    [InlineData(VirtualKey.Backslash, "\\")]
    [InlineData(VirtualKey.BackQuote, "`")]
    public void TryGetPrintableText_SpecialChars_ShouldReturnCorrectChar(VirtualKey key, string expected)
    {
        var result = InvokeTryGetPrintableText(key, KeyModifiers.None, out var text);

        Assert.True(result);
        Assert.Equal(expected, text);
    }

    [Theory]
    [InlineData(VirtualKey.Escape)]
    [InlineData(VirtualKey.Enter)]
    [InlineData(VirtualKey.Tab)]
    [InlineData(VirtualKey.Up)]
    [InlineData(VirtualKey.Down)]
    [InlineData(VirtualKey.Left)]
    [InlineData(VirtualKey.Right)]
    [InlineData(VirtualKey.F1)]
    public void TryGetPrintableText_NonPrintableKeys_ShouldReturnFalse(VirtualKey key)
    {
        var result = InvokeTryGetPrintableText(key, KeyModifiers.None, out var text);

        Assert.False(result);
        Assert.Equal(string.Empty, text);
    }

    [Fact]
    public void TryGetPrintableText_LetterWithCtrl_ShouldStillReturnChar()
    {
        var result = InvokeTryGetPrintableText(VirtualKey.A, KeyModifiers.Ctrl, out var text);

        Assert.True(result);
        Assert.Equal("a", text);
    }

    private static bool InvokeTryGetPrintableText(VirtualKey key, KeyModifiers modifiers, out string text)
    {
        var method = typeof(MainWindowOverlayService).GetMethod(
            "TryGetPrintableText",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Method TryGetPrintableText not found");

        var parameters = new object[] { key, modifiers, string.Empty };
        var result = (bool)method.Invoke(null, parameters)!;
        text = (string)parameters[2];
        return result;
    }
}

