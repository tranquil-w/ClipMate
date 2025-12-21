namespace ClipMate.Platform.Abstractions.Tray;

public interface ITrayIcon : IDisposable
{
    event EventHandler? Clicked;

    event EventHandler? DoubleClicked;

    string? ToolTip { get; set; }

    void Show();

    void Hide();

    void SetIcon(TrayIconSource iconSource);

    void SetContextMenu(IReadOnlyList<TrayMenuItem> menuItems);
}

