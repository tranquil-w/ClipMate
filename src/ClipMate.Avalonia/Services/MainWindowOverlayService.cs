using Avalonia;
using ClipMate.Avalonia.Infrastructure;
using ClipMate.Platform.Abstractions.Input;
using ClipMate.Service.Clipboard;
using ClipMate.UI.Abstractions;
using ClipMate.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ClipMate.Avalonia.Services;

public sealed class MainWindowOverlayService
{
    private readonly IKeyboardHook _keyboardHook;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPasteTargetWindowService _pasteTargetWindowService;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly ILogger _logger;
    private MainWindow? _mainWindow;
    private bool _initialized;
    private ClipboardViewModel? _clipboardViewModel;
    private NoActivateWindowController? _noActivateWindowController;
    private volatile bool _mainWindowVisible;

    public MainWindowOverlayService(
        IKeyboardHook keyboardHook,
        IServiceProvider serviceProvider,
        IPasteTargetWindowService pasteTargetWindowService,
        IUiDispatcher uiDispatcher,
        ILogger logger)
    {
        _keyboardHook = keyboardHook;
        _serviceProvider = serviceProvider;
        _pasteTargetWindowService = pasteTargetWindowService;
        _uiDispatcher = uiDispatcher;
        _logger = logger;
    }

    public void Initialize(MainWindow mainWindow)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _mainWindow = mainWindow;
        _noActivateWindowController = mainWindow.NoActivateWindowController;

        _keyboardHook.Start();
        _keyboardHook.KeyPressed += OnKeyboardHookKeyPressed;

        mainWindow.PropertyChanged += OnMainWindowPropertyChanged;
        UpdateState();

        _logger.Information("MainWindowOverlayService 初始化完成");
    }

    private void UpdateState()
    {
        if (_mainWindow == null)
        {
            return;
        }

        _mainWindowVisible = _mainWindow.IsVisible;

        if (_mainWindowVisible)
        {
            _pasteTargetWindowService.FreezePasteTarget();
            GetClipboardViewModel().OnWindowShown();
            return;
        }

        _pasteTargetWindowService.UnfreezePasteTarget();
        _noActivateWindowController?.ResumeNoActivate();
    }

    private void OnMainWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Visual.IsVisibleProperty)
        {
            UpdateState();
        }
    }

    private void OnKeyboardHookKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        if (!_mainWindowVisible)
        {
            return;
        }

        if (_noActivateWindowController?.IsNoActivateSuspended == true)
        {
            return;
        }

        var hasDisallowedModifiers =
            e.Modifiers.HasFlag(KeyModifiers.Ctrl) ||
            e.Modifiers.HasFlag(KeyModifiers.Alt) ||
            e.Modifiers.HasFlag(KeyModifiers.Win);

        switch (e.Key)
        {
            case VirtualKey.Escape:
                if (hasDisallowedModifiers)
                {
                    return;
                }

                _ = _uiDispatcher.InvokeAsync(() =>
                {
                    var viewModel = GetClipboardViewModel();
                    if (!string.IsNullOrEmpty(viewModel.SearchQuery))
                    {
                        viewModel.SearchQuery = string.Empty;
                        return;
                    }

                    _mainWindow?.Close();
                });
                e.Suppress = true;
                return;

            case VirtualKey.Up:
                if (hasDisallowedModifiers)
                {
                    return;
                }

                _ = _uiDispatcher.InvokeAsync(() => GetClipboardViewModel().SelectRelative(-1));
                e.Suppress = true;
                return;

            case VirtualKey.Down:
                if (hasDisallowedModifiers)
                {
                    return;
                }

                _ = _uiDispatcher.InvokeAsync(() => GetClipboardViewModel().SelectRelative(1));
                e.Suppress = true;
                return;

            case VirtualKey.Enter:
                if (hasDisallowedModifiers)
                {
                    return;
                }

                _ = _uiDispatcher.InvokeAsync(() =>
                {
                    var viewModel = GetClipboardViewModel();
                    if (viewModel.SelectedItem != null)
                    {
                        viewModel.PasteCommand.Execute(viewModel.SelectedItem);
                    }
                });
                e.Suppress = true;
                return;

            case VirtualKey.Space:
                if (hasDisallowedModifiers)
                {
                    return;
                }

                _ = _uiDispatcher.InvokeAsync(() => GetClipboardViewModel().RequestSearchBoxFocus());
                e.Suppress = true;
                return;

            case VirtualKey.Backspace:
            case VirtualKey.Delete:
                if (hasDisallowedModifiers)
                {
                    return;
                }

                _ = _uiDispatcher.InvokeAsync(() => GetClipboardViewModel().BackspaceSearchText());
                e.Suppress = true;
                return;
        }
    }

    private ClipboardViewModel GetClipboardViewModel()
    {
        return _clipboardViewModel ??= _serviceProvider.GetRequiredService<ClipboardViewModel>();
    }
}
