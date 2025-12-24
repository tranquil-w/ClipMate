using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using ClipMate.Avalonia.Views;
using ClipMate.UI.Abstractions;

namespace ClipMate.Avalonia.Services;

public sealed class AvaloniaUserDialogService : IUserDialogService
{
    public async Task<bool> ConfirmAsync(string message, string title)
    {
        var dialog = new MessageDialog(message, title, showCancel: true);
        return await dialog.ShowDialogAsync(GetOwner());
    }

    public async Task ShowErrorAsync(string message, string title)
    {
        var dialog = new MessageDialog(message, title, showCancel: false);
        _ = await dialog.ShowDialogAsync(GetOwner());
    }

    private static Window? GetOwner()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return null;
        }

        foreach (var window in desktop.Windows)
        {
            if (window.IsActive && window.IsVisible)
            {
                return window;
            }
        }

        if (desktop.MainWindow?.IsVisible == true)
        {
            return desktop.MainWindow;
        }

        foreach (var window in desktop.Windows)
        {
            if (window.IsVisible)
            {
                return window;
            }
        }

        return null;
    }
}
