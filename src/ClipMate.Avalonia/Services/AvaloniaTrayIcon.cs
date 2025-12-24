using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Native;
using Avalonia.Platform;
using ClipMate.Platform.Abstractions.Tray;
using Serilog;

namespace ClipMate.Avalonia.Services;

public sealed class AvaloniaTrayIcon : ITrayIcon
{
    private readonly ILogger _logger;
    private readonly TrayIcon _trayIcon;
    private bool _disposed;

    public event EventHandler? Clicked;
    public event EventHandler? DoubleClicked;

    public AvaloniaTrayIcon(ILogger logger)
    {
        _logger = logger;
        _trayIcon = new TrayIcon();
        _trayIcon.Clicked += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);
    }

    public string? ToolTip
    {
        get => _trayIcon.ToolTipText;
        set => _trayIcon.ToolTipText = value;
    }

    public void Show()
    {
        _trayIcon.IsVisible = true;
    }

    public void Hide()
    {
        _trayIcon.IsVisible = false;
    }

    public void SetIcon(TrayIconSource iconSource)
    {
        if (_disposed)
        {
            return;
        }

        switch (iconSource)
        {
            case TrayIconSource.ResourceUri resource:
                _trayIcon.Icon = LoadWindowIcon(resource.Uri);
                break;
            case TrayIconSource.FilePath file:
                _trayIcon.Icon = new WindowIcon(file.Path);
                break;
            case TrayIconSource.PngBytes png:
                _trayIcon.Icon = LoadWindowIcon(png.Bytes);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(iconSource));
        }
    }

    public void SetContextMenu(IReadOnlyList<TrayMenuItem> menuItems)
    {
        if (_disposed)
        {
            return;
        }

        var menu = new NativeMenu();
        foreach (var item in menuItems)
        {
            menu.Items.Add(BuildMenuItem(item));
        }

        _trayIcon.Menu = menu;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        try
        {
            _trayIcon.Dispose();
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "释放 AvaloniaTrayIcon 资源失败");
        }
    }

    private static NativeMenuItemBase BuildMenuItem(TrayMenuItem item)
    {
        if (item.IsSeparator)
        {
            return new NativeMenuItemSeparator();
        }

        var menuItem = new NativeMenuItem(item.Title ?? string.Empty)
        {
            IsEnabled = item.IsEnabled
        };

        if (item.Children is { Count: > 0 })
        {
            var submenu = new NativeMenu();
            foreach (var child in item.Children)
            {
                submenu.Items.Add(BuildMenuItem(child));
            }
            menuItem.Menu = submenu;
        }
        else if (item.OnClick != null)
        {
            menuItem.Click += (_, _) => item.OnClick.Invoke();
        }

        return menuItem;
    }

    private static WindowIcon LoadWindowIcon(string uri)
    {
        var assetUri = new Uri(uri);
        using var stream = AssetLoader.Open(assetUri);
        return new WindowIcon(stream);
    }

    private static WindowIcon LoadWindowIcon(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return new WindowIcon(stream);
    }
}
