using ClipMate.Platform.Abstractions.Input;

namespace ClipMate.Platform.Abstractions.Tests.Input;

public class VirtualKeyTests
{
    [Fact]
    public void VirtualKey_ShouldMatchExpectedHidSubsetValues()
    {
        Assert.Equal(4, (int)VirtualKey.A);
        Assert.Equal(30, (int)VirtualKey.D1);
        Assert.Equal(39, (int)VirtualKey.D0);
        Assert.Equal(58, (int)VirtualKey.F1);
        Assert.Equal(40, (int)VirtualKey.Enter);
        Assert.Equal(41, (int)VirtualKey.Escape);
    }
}

