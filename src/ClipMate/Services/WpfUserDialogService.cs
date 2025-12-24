using ClipMate.UI.Abstractions;
using HandyMessageBox = HandyControl.Controls.MessageBox;
using System.Windows;

namespace ClipMate.Services;

public sealed class WpfUserDialogService : IUserDialogService
{
    public Task<bool> ConfirmAsync(string message, string title)
    {
        var result = HandyMessageBox.Show(
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    public Task ShowErrorAsync(string message, string title)
    {
        HandyMessageBox.Show(
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        return Task.CompletedTask;
    }
}
