using ClipMate.Platform.Abstractions.Window;
using System.Windows;
using System.Windows.Media;

namespace ClipMate.Platform.Windows.Windowing;

public sealed class WpfMainWindowController : IMainWindowController
{
    public void CloseMainWindow()
    {
        Application.Current?.MainWindow?.Close();
    }

    public void ShowMainWindow()
    {
        var window = Application.Current?.MainWindow;
        if (window == null)
        {
            return;
        }

        if (!window.IsVisible)
        {
            window.Show();
            return;
        }

        window.Show();
    }

    public void HideMainWindow()
    {
        Application.Current?.MainWindow?.Hide();
    }

    public Task HideMainWindowAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var window = Application.Current?.MainWindow;
        if (window == null || !window.IsVisible)
        {
            return Task.CompletedTask;
        }

        var dispatcher = window.Dispatcher;
        if (dispatcher.CheckAccess())
        {
            return HideAndWaitAsync(window, cancellationToken);
        }

        return dispatcher.InvokeAsync(() => HideAndWaitAsync(window, cancellationToken)).Task.Unwrap();
    }

    public void SetPosition(ScreenPoint position)
    {
        var window = Application.Current?.MainWindow;
        if (window == null)
        {
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(window);
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = position.X / dpi.DpiScaleX;
        window.Top = position.Y / dpi.DpiScaleY;
    }

    private static Task HideAndWaitAsync(Window window, CancellationToken cancellationToken)
    {
        if (!window.IsVisible)
        {
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnIsVisibleChanged(object? _, DependencyPropertyChangedEventArgs __)
        {
            if (!window.IsVisible)
            {
                window.IsVisibleChanged -= OnIsVisibleChanged;
                tcs.TrySetResult();
            }
        }

        window.IsVisibleChanged += OnIsVisibleChanged;

        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() =>
            {
                window.Dispatcher.BeginInvoke(() => window.IsVisibleChanged -= OnIsVisibleChanged);
                tcs.TrySetCanceled(cancellationToken);
            });
        }

        try
        {
            window.Close();
        }
        catch
        {
            window.IsVisibleChanged -= OnIsVisibleChanged;
            throw;
        }

        if (!window.IsVisible)
        {
            window.IsVisibleChanged -= OnIsVisibleChanged;
            return Task.CompletedTask;
        }

        return tcs.Task;
    }
}
