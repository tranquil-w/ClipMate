using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ClipMate.Avalonia.Views;

public partial class MessageDialog : Window
{
    private readonly TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public MessageDialog(string message, string title, bool showCancel)
    {
        InitializeComponent();
        Title = title;
        Message = message;
        ShowCancel = showCancel;
        ConfirmText = "确定";
        CancelText = "取消";
        DataContext = this;
    }

    public string Message { get; }

    public string ConfirmText { get; }

    public string CancelText { get; }

    public bool ShowCancel { get; }

    public Task<bool> ShowDialogAsync(Window? owner)
    {
        if (owner != null)
        {
            _ = ShowDialog(owner);
        }
        else
        {
            Show();
        }

        return _tcs.Task;
    }

    private void ConfirmButton_Click(object? sender, RoutedEventArgs e)
    {
        _tcs.TrySetResult(true);
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        _tcs.TrySetResult(false);
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (!_tcs.Task.IsCompleted)
        {
            _tcs.TrySetResult(false);
        }

        base.OnClosed(e);
    }
}
