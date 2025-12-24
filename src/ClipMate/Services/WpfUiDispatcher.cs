using ClipMate.UI.Abstractions;
using System.Windows;
using System.Windows.Threading;

namespace ClipMate.Services;

public sealed class WpfUiDispatcher : IUiDispatcher
{
    public bool CheckAccess()
    {
        var dispatcher = Application.Current?.Dispatcher;
        return dispatcher == null || dispatcher.CheckAccess();
    }

    public void Invoke(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }

    public Task InvokeAsync(Action action, UiDispatcherPriority priority = UiDispatcherPriority.Normal, CancellationToken cancellationToken = default)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(action, MapPriority(priority), cancellationToken).Task;
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
