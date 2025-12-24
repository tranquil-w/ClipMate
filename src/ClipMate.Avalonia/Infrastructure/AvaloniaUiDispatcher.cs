using Avalonia.Threading;
using ClipMate.UI.Abstractions;

namespace ClipMate.Avalonia.Infrastructure;

public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public bool CheckAccess()
    {
        return Dispatcher.UIThread.CheckAccess();
    }

    public void Invoke(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return;
        }

        Dispatcher.UIThread.InvokeAsync(action);
    }

    public Task InvokeAsync(
        Action action,
        UiDispatcherPriority priority = UiDispatcherPriority.Normal,
        CancellationToken cancellationToken = default)
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(() =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
                return;
            }

            try
            {
                action();
                tcs.TrySetResult(null);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, MapPriority(priority));

        return tcs.Task;
    }

    private static DispatcherPriority MapPriority(UiDispatcherPriority priority)
    {
        return priority switch
        {
            UiDispatcherPriority.Background => DispatcherPriority.Background,
            UiDispatcherPriority.High => DispatcherPriority.Send,
            _ => DispatcherPriority.Normal
        };
    }
}
