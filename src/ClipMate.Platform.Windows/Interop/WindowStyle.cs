using System.Runtime.InteropServices;

namespace ClipMate.Interop;

internal static partial class WindowStyle
{
#pragma warning disable IDE1006 // 命名样式
    private const uint WS_EX_NOACTIVATE = 0x08000000;
    private const int GWL_EXSTYLE = -20;
    private const int GWL_STYLE = -16;
    private const uint WS_SYSMENU = 0x00080000;

    internal const int WM_MOUSEACTIVATE = 0x0021;
    internal const int MA_NOACTIVATE = 0x0003;
    internal const int SW_SHOWNA = 0x0008;
#pragma warning restore IDE1006 // 命名样式

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(nint hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ShowWindow(nint hWnd, int nCmdShow);

    /// <summary>
    /// 设置窗口样式，使其不获取焦点
    /// </summary>
    /// <param name="hWnd"></param>
    internal static void SetUnfocusable(nint hWnd)
    {
        var style = GetWindowLong(hWnd, GWL_EXSTYLE);
        _ = SetWindowLong(hWnd, GWL_EXSTYLE, style | WS_EX_NOACTIVATE);
    }

    /// <summary>
    /// 移除窗口的系统菜单，防止Alt键触发系统菜单
    /// </summary>
    /// <param name="hWnd">窗口句柄</param>
    internal static void RemoveSystemMenu(nint hWnd)
    {
        var style = GetWindowLong(hWnd, GWL_STYLE);
        _ = SetWindowLong(hWnd, GWL_STYLE, style & ~WS_SYSMENU);
    }

    /// <summary>
    /// 移除窗口的无焦点样式，使其可以获取焦点
    /// </summary>
    internal static void SetFocusable(nint hWnd)
    {
        var style = GetWindowLong(hWnd, GWL_EXSTYLE);
        _ = SetWindowLong(hWnd, GWL_EXSTYLE, style & ~WS_EX_NOACTIVATE);
    }

    /// <summary>
    /// 检查窗口是否设置了无焦点样式
    /// </summary>
    internal static bool IsUnfocusable(nint hWnd)
    {
        var style = GetWindowLong(hWnd, GWL_EXSTYLE);
        return (style & WS_EX_NOACTIVATE) != 0;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(nint hWnd);
}
