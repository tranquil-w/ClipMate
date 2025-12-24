using Avalonia;
using Avalonia.Controls;
using ClipMate.Avalonia.Infrastructure;
using ClipMate.Platform.Abstractions.Window;
using ClipMate.UI.Abstractions;

namespace ClipMate.Avalonia.Services;

public sealed class AvaloniaMainWindowController : IMainWindowController
{
    private readonly Func<MainWindow> _mainWindowFactory;
    private readonly IUiDispatcher _uiDispatcher;

    public AvaloniaMainWindowController(Func<MainWindow> mainWindowFactory, IUiDispatcher uiDispatcher)
    {
        _mainWindowFactory = mainWindowFactory;
        _uiDispatcher = uiDispatcher;
    }

    public void CloseMainWindow()
    {
        _uiDispatcher.Invoke(() => _mainWindowFactory().Close());
    }

    public void ShowMainWindow()
    {
        _uiDispatcher.Invoke(() => NoActivateWindowController.ShowNoActivate(_mainWindowFactory()));
    }

    public void HideMainWindow()
    {
        _uiDispatcher.Invoke(() => _mainWindowFactory().Hide());
    }

    public Task HideMainWindowAsync(CancellationToken cancellationToken = default)
    {
        return _uiDispatcher.InvokeAsync(() => _mainWindowFactory().Hide(), cancellationToken: cancellationToken);
    }

    public void SetPosition(ScreenPoint position)
    {
        _uiDispatcher.Invoke(() =>
        {
            var mainWindow = _mainWindowFactory();
            mainWindow.Position = new PixelPoint(position.X, position.Y);
        });
    }
}
