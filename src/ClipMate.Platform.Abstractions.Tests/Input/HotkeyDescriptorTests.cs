using ClipMate.Platform.Abstractions.Input;

namespace ClipMate.Platform.Abstractions.Tests.Input;

public class HotkeyDescriptorTests
{
    [Fact]
    public void Parse_ShouldHandleCommonHotkeyStrings()
    {
        var hotkey = HotkeyDescriptor.Parse("Ctrl+Shift+V");

        Assert.Equal(VirtualKey.V, hotkey.Key);
        Assert.True(hotkey.Modifiers.HasFlag(KeyModifiers.Ctrl));
        Assert.True(hotkey.Modifiers.HasFlag(KeyModifiers.Shift));
        Assert.False(hotkey.Modifiers.HasFlag(KeyModifiers.Alt));
        Assert.Equal("Ctrl + Shift + V", hotkey.DisplayString);
    }

    [Fact]
    public void Parse_ShouldHandleBackQuoteVariants()
    {
        var a = HotkeyDescriptor.Parse("Ctrl + `");
        var b = HotkeyDescriptor.Parse("Ctrl+~");

        Assert.Equal(VirtualKey.BackQuote, a.Key);
        Assert.True(a.Modifiers.HasFlag(KeyModifiers.Ctrl));

        Assert.Equal(VirtualKey.BackQuote, b.Key);
        Assert.True(b.Modifiers.HasFlag(KeyModifiers.Ctrl));
    }

    [Fact]
    public void TryParse_ShouldRejectUnset()
    {
        Assert.False(HotkeyDescriptor.TryParse("未设置", out _));
        Assert.False(HotkeyDescriptor.TryParse(null, out _));
        Assert.False(HotkeyDescriptor.TryParse("  ", out _));
    }
}

