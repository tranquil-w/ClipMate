using Avalonia.Controls;
using Avalonia.Platform;
using System.Runtime.InteropServices;

namespace ClipMate.Avalonia.Infrastructure;

public sealed class NoActivateWindowController
{
    private readonly Window _window;
    private nint _hwnd;
    private bool _isNoActivateSuspended;

    public NoActivateWindowController(Window window)
    {
        _window = window;
    }

    public bool IsNoActivateSuspended => _isNoActivateSuspended;

    public void Attach()
    {
        _window.Opened += (_, _) =>
        {
            _hwnd = GetWindowHandle();
            if (_hwnd != nint.Zero)
            {
                SetUnfocusable(_hwnd);
            }
        };
    }

    public static void ShowNoActivate(Window window)
    {
        if (!window.IsVisible)
        {
            window.Show();
        }

        var handle = window.TryGetPlatformHandle();
        if (handle?.Handle is { } hwnd && hwnd != nint.Zero)
        {
            SetUnfocusable(hwnd);
            _ = ShowWindow(hwnd, SW_SHOWNA);
        }
    }

    public void SuspendNoActivate()
    {
        if (_isNoActivateSuspended || _hwnd == nint.Zero)
        {
            return;
        }

        SetFocusable(_hwnd);
        _isNoActivateSuspended = true;
    }

    public void ResumeNoActivate()
    {
        if (!_isNoActivateSuspended || _hwnd == nint.Zero)
        {
            return;
        }

        SetUnfocusable(_hwnd);
        _isNoActivateSuspended = false;
    }

    private nint GetWindowHandle()
    {
        var handle = _window.TryGetPlatformHandle();
        return handle?.Handle ?? nint.Zero;
    }

#pragma warning disable IDE1006
    private const uint WS_EX_NOACTIVATE = 0x08000000;
    private const int GWL_EXSTYLE = -20;
    private const int SW_SHOWNA = 0x0008;
#pragma warning restore IDE1006

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(nint hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    private static void SetUnfocusable(nint hWnd)
    {
        var style = GetWindowLong(hWnd, GWL_EXSTYLE);
        _ = SetWindowLong(hWnd, GWL_EXSTYLE, style | WS_EX_NOACTIVATE);
    }

    private static void SetFocusable(nint hWnd)
    {
        var style = GetWindowLong(hWnd, GWL_EXSTYLE);
        _ = SetWindowLong(hWnd, GWL_EXSTYLE, style & ~WS_EX_NOACTIVATE);
    }
}
