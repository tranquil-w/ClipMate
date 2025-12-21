using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using InteropWindowStyle = ClipMate.Interop.WindowStyle;

namespace ClipMate.Infrastructure;

internal sealed class NoActivateWindowController
{
    private readonly Window _window;
    private HwndSource? _source;
    private bool _ignoreUntilButtonUp;
    private bool _isNoActivateSuspended;

    // 外部点击检测：低级鼠标钩子（事件驱动，替代轮询）
    private nint _mouseHook;
    private HookProc? _mouseHookProc;
    private bool _mouseHookInstalled;

    /// <summary>
    /// 获取无焦点模式是否被暂停
    /// </summary>
    internal bool IsNoActivateSuspended => _isNoActivateSuspended;

    internal NoActivateWindowController(Window window)
    {
        _window = window;
    }

    internal void Attach()
    {
        var hwnd = new WindowInteropHelper(_window).Handle;
        if (hwnd == nint.Zero)
        {
            return;
        }

        InteropWindowStyle.SetUnfocusable(hwnd);

        _source = HwndSource.FromHwnd(hwnd);
        _source?.AddHook(WndProc);

        _window.IsVisibleChanged += (_, _) => UpdateOutsideClickWatcher();
        _window.Closed += (_, _) => RemoveOutsideClickHook();
        UpdateOutsideClickWatcher();
    }

    internal static void ShowNoActivate(Window window)
    {
        window.ShowActivated = false;
        if (!window.IsVisible)
        {
            window.Show();
        }

        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd != nint.Zero)
        {
            InteropWindowStyle.ShowWindow(hwnd, InteropWindowStyle.SW_SHOWNA);
        }
    }

    /// <summary>
    /// 暂停无焦点模式，允许窗口获取焦点（用于搜索框输入）
    /// </summary>
    internal void SuspendNoActivate()
    {
        if (_isNoActivateSuspended)
        {
            return;
        }

        var hwnd = new WindowInteropHelper(_window).Handle;
        if (hwnd == nint.Zero)
        {
            return;
        }

        InteropWindowStyle.SetFocusable(hwnd);
        _isNoActivateSuspended = true;
    }

    /// <summary>
    /// 恢复无焦点模式
    /// </summary>
    internal void ResumeNoActivate()
    {
        if (!_isNoActivateSuspended)
        {
            return;
        }

        var hwnd = new WindowInteropHelper(_window).Handle;
        if (hwnd == nint.Zero)
        {
            return;
        }

        InteropWindowStyle.SetUnfocusable(hwnd);
        _isNoActivateSuspended = false;
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        // 当无焦点模式暂停时，不拦截消息
        if (_isNoActivateSuspended)
        {
            return nint.Zero;
        }

        if (msg == InteropWindowStyle.WM_MOUSEACTIVATE)
        {
            handled = true;
            return new nint(InteropWindowStyle.MA_NOACTIVATE);
        }

        return nint.Zero;
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

        if (!_window.IsVisible)
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

            _ = _window.Dispatcher.InvokeAsync(_window.Close);
        }
        catch
        {
            // best-effort: ignore hook errors
        }

        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private bool IsPointInsideWindow(POINT point)
    {
        var hwnd = new WindowInteropHelper(_window).Handle;
        if (hwnd == nint.Zero)
        {
            return false;
        }

        if (!GetWindowRect(hwnd, out var rect))
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

    private const int VK_LBUTTON = 0x01;
    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(int idHook, HookProc lpfn, nint hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern nint GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);       

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct POINT
    {
        public readonly int X;
        public readonly int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct MSLLHOOKSTRUCT
    {
        public readonly POINT pt;
        public readonly uint mouseData;
        public readonly uint flags;
        public readonly uint time;
        public readonly nuint dwExtraInfo;
    }

    private delegate nint HookProc(int nCode, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct RECT
    {
        public readonly int Left;
        public readonly int Top;
        public readonly int Right;
        public readonly int Bottom;
    }
}
