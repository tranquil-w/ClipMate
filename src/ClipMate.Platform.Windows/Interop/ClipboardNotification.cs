using System.Runtime.InteropServices;

namespace ClipMate.Platform.Windows.Interop;

internal static partial class ClipboardNotification
{
    internal const int WM_DRAWCLIPBOARD = 0x0308;
    private static nint _nextClipboardViewer;

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial nint SetClipboardViewer(nint hWndNewViewer);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ChangeClipboardChain(nint hWndRemove, nint hWndNewNext);

    internal static void RegisterClipboardViewer(nint hwnd)
    {
        _nextClipboardViewer = SetClipboardViewer(hwnd);
    }

    internal static void UnregisterClipboardViewer(nint hwnd)
    {
        ChangeClipboardChain(hwnd, _nextClipboardViewer);
    }
}

