using System.Runtime.InteropServices;

namespace ClipMate.Platform.Windows.Interop;

internal static class WindowSwitchNative
{
    [DllImport("user32.dll")]
    internal static extern nint GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern nint FindWindow(string lpClassName, string? lpWindowName);
}

