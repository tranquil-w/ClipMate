using ClipMate.Platform.Abstractions.Window;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ClipMate.Platform.Windows.Windowing;

public sealed class WindowsForegroundWindowTracker : IForegroundWindowTracker
{
    private readonly int _currentProcessId = Process.GetCurrentProcess().Id;
    private readonly object _gate = new();
    private WinEventDelegate? _hookCallback;
    private nint _hookHandle;
    private bool _started;
    private nint _currentForeground;
    private nint _lastExternalForeground;

    public event EventHandler<nint>? ForegroundWindowChanged;

    public nint CurrentForegroundWindowHandle
    {
        get
        {
            lock (_gate)
            {
                return _currentForeground;
            }
        }
    }

    public nint LastExternalForegroundWindowHandle
    {
        get
        {
            lock (_gate)
            {
                return _lastExternalForeground;
            }
        }
    }

    public bool IsWindowFromCurrentProcess(nint windowHandle)
    {
        if (windowHandle == nint.Zero)
        {
            return false;
        }

        _ = GetWindowThreadProcessId(windowHandle, out var pid);
        return pid == (uint)_currentProcessId;
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_started)
            {
                return;
            }

            _hookCallback = OnWinEvent;
            _hookHandle = SetWinEventHook(
                EVENT_SYSTEM_FOREGROUND,
                EVENT_SYSTEM_FOREGROUND,
                nint.Zero,
                _hookCallback,
                0,
                0,
                WINEVENT_OUTOFCONTEXT);

            _currentForeground = GetForegroundWindow();
            if (!IsWindowFromCurrentProcess(_currentForeground))
            {
                _lastExternalForeground = _currentForeground;
            }

            _started = true;
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            if (!_started)
            {
                return;
            }

            if (_hookHandle != nint.Zero)
            {
                _ = UnhookWinEvent(_hookHandle);
            }

            _hookHandle = nint.Zero;
            _hookCallback = null;
            _started = false;
        }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    private void OnWinEvent(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (eventType != EVENT_SYSTEM_FOREGROUND)
        {
            return;
        }

        if (hwnd == nint.Zero)
        {
            return;
        }

        nint current;
        nint lastExternal;
        lock (_gate)
        {
            _currentForeground = hwnd;
            if (!IsWindowFromCurrentProcess(hwnd))
            {
                _lastExternalForeground = hwnd;
            }

            current = _currentForeground;
            lastExternal = _lastExternalForeground;
        }

        // 回调线程不保证为 UI 线程，事件订阅者自行决定调度策略
        ForegroundWindowChanged?.Invoke(this, current == nint.Zero ? lastExternal : current);
    }

    private delegate void WinEventDelegate(
        nint hWinEventHook,
        uint eventType,
        nint hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime);

    private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    [DllImport("user32.dll")]
    private static extern nint SetWinEventHook(
        uint eventMin,
        uint eventMax,
        nint hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(nint hWinEventHook);

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);
}
