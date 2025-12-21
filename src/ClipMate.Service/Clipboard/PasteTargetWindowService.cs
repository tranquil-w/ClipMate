using ClipMate.Platform.Abstractions.Window;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ClipMate.Service.Clipboard;

public sealed class PasteTargetWindowService(IForegroundWindowTracker foregroundWindowTracker, ILogger logger) : IPasteTargetWindowService
{
    private readonly IForegroundWindowTracker _foregroundWindowTracker = foregroundWindowTracker;
    private readonly ILogger _logger = logger;
    private readonly object _gate = new();
    private bool _frozen;
    private nint _pasteTargetWindowHandle;

    public nint PasteTargetWindowHandle
    {
        get
        {
            lock (_gate)
            {
                return _pasteTargetWindowHandle;
            }
        }
    }

    public void FreezePasteTarget()
    {
        lock (_gate)
        {
            if (_frozen)
            {
                return;
            }

            var candidate = _foregroundWindowTracker.LastExternalForegroundWindowHandle;
            if (candidate == nint.Zero)
            {
                candidate = _foregroundWindowTracker.CurrentForegroundWindowHandle;
            }

            _pasteTargetWindowHandle = candidate;
            _frozen = true;
            _logger.Debug("粘贴目标已冻结：Hwnd={Hwnd}", _pasteTargetWindowHandle);
        }
    }

    public void UnfreezePasteTarget()
    {
        lock (_gate)
        {
            _frozen = false;
            _pasteTargetWindowHandle = nint.Zero;
        }
    }

    public async Task<(bool Ready, nint CurrentForeground)> WaitForReadyToPasteAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var pasteTarget = PasteTargetWindowHandle;
        var currentForeground = _foregroundWindowTracker.CurrentForegroundWindowHandle;
        if (IsReady(pasteTarget, currentForeground))
        {
            return (true, currentForeground);
        }

        var tcs = new TaskCompletionSource<nint>(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handler(object? _, nint hwnd)
        {
            var current = _foregroundWindowTracker.CurrentForegroundWindowHandle;
            if (IsReady(pasteTarget, current))
            {
                tcs.TrySetResult(current);
            }
        }

        _foregroundWindowTracker.ForegroundWindowChanged += Handler;

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            var readyForeground = await tcs.Task.WaitAsync(timeoutCts.Token);
            return (true, readyForeground);
        }
        catch (OperationCanceledException)
        {
            return (false, _foregroundWindowTracker.CurrentForegroundWindowHandle);
        }
        finally
        {
            _foregroundWindowTracker.ForegroundWindowChanged -= Handler;
        }
    }

    private bool IsReady(nint pasteTarget, nint currentForeground)
    {
        if (pasteTarget != nint.Zero && currentForeground == pasteTarget)
        {
            return true;
        }

        // 保守退化：至少确保前台不是 ClipMate（同进程窗口）
        return currentForeground != nint.Zero && !_foregroundWindowTracker.IsWindowFromCurrentProcess(currentForeground);
    }
}
