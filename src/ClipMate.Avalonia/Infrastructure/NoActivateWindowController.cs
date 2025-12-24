using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using System.Runtime.InteropServices;

namespace ClipMate.Avalonia.Infrastructure;

public sealed class NoActivateWindowController
{
    private readonly Window _window;
    private nint _hwnd;
    private bool _isNoActivateSuspended;
    private bool _ignoreUntilButtonUp;
    private bool _isOutsideClickSuppressed;
    private nint _mouseHook;
    private HookProc? _mouseHookProc;
    private bool _mouseHookInstalled;

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
        _window.PropertyChanged += OnWindowPropertyChanged;
        _window.Closed += (_, _) => RemoveOutsideClickHook();
        UpdateOutsideClickWatcher();
    }

    public static void ShowNoActivate(Window window)
    {
        window.ShowActivated = false;
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

    public void SetOutsideClickSuppressed(bool suppressed)
    {
        _isOutsideClickSuppressed = suppressed;
    }

    private nint GetWindowHandle()
    {
        var handle = _window.TryGetPlatformHandle();
        return handle?.Handle ?? nint.Zero;
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Visual.IsVisibleProperty)
        {
            UpdateOutsideClickWatcher();
        }
    }

    private void UpdateOutsideClickWatcher()
    {
        if (!_window.IsVisible)
        {
            RemoveOutsideClickHook();
            return;
        }

        _ignoreUntilButtonUp = IsLeftButtonDown();
        InstallOutsideClickHook();
    }

    private void InstallOutsideClickHook()
    {
        if (_mouseHookInstalled)
        {
            return;
        }

        _mouseHookProc = MouseHookCallback;
        _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProc, GetModuleHandle(null), 0);
        if (_mouseHook == nint.Zero)
        {
            _mouseHookProc = null;
            return;
        }

        _mouseHookInstalled = true;
    }

    private void RemoveOutsideClickHook()
    {
        if (!_mouseHookInstalled)
        {
            return;
        }

        try
        {
            _ = UnhookWindowsHookEx(_mouseHook);
        }
        finally
        {
            _mouseHook = nint.Zero;
            _mouseHookProc = null;
            _mouseHookInstalled = false;
        }
    }

    private nint MouseHookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode < 0)
        {
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        if (!_window.IsVisible || _isOutsideClickSuppressed)
        {
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        var message = (int)wParam;
        if (_ignoreUntilButtonUp)
        {
            if (message == WM_LBUTTONUP)
            {
                _ignoreUntilButtonUp = false;
            }

            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        if (message != WM_LBUTTONDOWN)
        {
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        try
        {
            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            if (IsPointInsideWindow(hookStruct.pt))
            {
                return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
            }

            Dispatcher.UIThread.Post(_window.Close);
        }
        catch
        {
            // best-effort: ignore hook errors
        }

        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private bool IsPointInsideWindow(POINT point)
    {
        if (_hwnd == nint.Zero && (_hwnd = GetWindowHandle()) == nint.Zero)
        {
            return false;
        }

        if (!GetWindowRect(_hwnd, out var rect))
        {
            return false;
        }

        return point.X >= rect.Left &&
               point.X <= rect.Right &&
               point.Y >= rect.Top &&
               point.Y <= rect.Bottom;
    }

    private static bool IsLeftButtonDown()
    {
        return (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
    }

#pragma warning disable IDE1006
    private const uint WS_EX_NOACTIVATE = 0x08000000;
    private const int GWL_EXSTYLE = -20;
    private const int SW_SHOWNA = 0x0008;
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int VK_LBUTTON = 0x01;

    private delegate nint HookProc(int nCode, nint wParam, nint lParam);
#pragma warning restore IDE1006

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(nint hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, HookProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GetModuleHandle(string? lpModuleName);

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

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public int mouseData;
        public int flags;
        public int time;
        public nint dwExtraInfo;
    }
}
