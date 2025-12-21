using ClipMate.Platform.Abstractions.Tray;
using HandyControl.Controls;
using Serilog;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ClipMate.Platform.Windows.Tray;

public sealed class WindowsTrayIcon : ITrayIcon
{
    private readonly ILogger _logger;
    private readonly NotifyIcon _notifyIcon;
    private bool _disposed;

    public event EventHandler? Clicked;
    public event EventHandler? DoubleClicked;

    public WindowsTrayIcon(ILogger logger)
    {
        _logger = logger;
        _notifyIcon = new NotifyIcon
        {
            Visibility = Visibility.Visible
        };

        _notifyIcon.Click += OnClick;

        _notifyIcon.Init();
    }

    public string? ToolTip
    {
        get => _notifyIcon.ToolTip?.ToString();
        set => _notifyIcon.ToolTip = value;
    }

    public void Show()
    {
        _notifyIcon.Visibility = Visibility.Visible;
    }

    public void Hide()
    {
        _notifyIcon.Visibility = Visibility.Collapsed;
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
                _notifyIcon.Icon = LoadIcon(resource.Uri);
                break;
            case TrayIconSource.FilePath file:
                _notifyIcon.Icon = LoadIcon(Path.GetFullPath(file.Path));
                break;
            case TrayIconSource.PngBytes:
                throw new NotSupportedException("WindowsTrayIcon 暂不支持从 PNG 字节设置图标，请使用 ResourceUri/FilePath");
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

        var menu = new ContextMenu();
        foreach (var item in menuItems)
        {
            menu.Items.Add(BuildMenuItem(item));
        }

        _notifyIcon.ContextMenu = menu;
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
            _notifyIcon.Click -= OnClick;
            _notifyIcon.Dispose();
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "释放 WindowsTrayIcon 资源失败");
        }
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        Clicked?.Invoke(this, EventArgs.Empty);
    }

    private static object BuildMenuItem(TrayMenuItem item)
    {
        if (item.IsSeparator)
        {
            return new Separator();
        }

        var menuItem = new MenuItem
        {
            Header = item.Title ?? string.Empty,
            IsEnabled = item.IsEnabled
        };

        if (item.Children is { Count: > 0 })
        {
            foreach (var child in item.Children)
            {
                menuItem.Items.Add(BuildMenuItem(child));
            }
        }
        else if (item.OnClick != null)
        {
            menuItem.Click += (_, _) => item.OnClick.Invoke();
        }

        return menuItem;
    }

    private static ImageSource LoadIcon(string uriOrPath)
    {
        var uri = Uri.TryCreate(uriOrPath, UriKind.Absolute, out var absolute)
            ? absolute
            : new Uri(uriOrPath, UriKind.RelativeOrAbsolute);

        var image = BitmapFrame.Create(uri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        image.Freeze();
        return image;
    }
}
