using ClipMate.Platform.Abstractions.Window;
using ClipMate.Platform.Windows.Interop;

namespace ClipMate.Platform.Windows.Windowing;

public sealed class WindowsForegroundWindowService : IForegroundWindowService
{
    public nint GetForegroundWindowHandle()
    {
        return WindowSwitchNative.GetForegroundWindow();
    }
}

