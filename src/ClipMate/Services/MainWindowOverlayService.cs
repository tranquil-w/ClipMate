using ClipMate.Service.Interfaces;
using ClipMate.Service.Clipboard;
using ClipMate.Infrastructure;
using ClipMate.Platform.Abstractions.Input;
using Serilog;
using Prism.Ioc;
using System.Windows;

namespace ClipMate.Services;

public sealed class MainWindowOverlayService
{
    private readonly IKeyboardHook _keyboardHook;
    private readonly IContainerProvider _container;
    private readonly ILogger _logger;
    private readonly IPasteTargetWindowService _pasteTargetWindowService;
    private Window? _mainWindow;
    private bool _initialized;
    private ClipMate.ViewModels.ClipboardViewModel? _clipboardViewModel;
    private NoActivateWindowController? _noActivateWindowController;
    private volatile bool _mainWindowVisible;

    public MainWindowOverlayService(
        IKeyboardHook keyboardHook,
        IContainerProvider container,
        IPasteTargetWindowService pasteTargetWindowService,
        ILogger logger)
    {
        _keyboardHook = keyboardHook;
        _container = container;
        _pasteTargetWindowService = pasteTargetWindowService;
        _logger = logger;
    }

    public void Initialize(Window mainWindow)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _mainWindow = mainWindow;

        // 获取 NoActivateWindowController 实例
        if (_mainWindow is MainWindow mw)
        {
            _noActivateWindowController = mw.NoActivateWindowController;
        }

        _keyboardHook.Start();
        _keyboardHook.KeyPressed += OnKeyboardHookKeyPressed;

        _mainWindow.IsVisibleChanged += (_, _) => UpdateState();
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

        // 确保在窗口隐藏/关闭后恢复无焦点模式，避免下次唤起时意外抢焦点
        _noActivateWindowController?.ResumeNoActivate();
    }

    private void OnKeyboardHookKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        if (!_mainWindowVisible)
        {
            return;
        }

        // 当窗口处于激活模式时（搜索框正在输入），不拦截键盘输入，让 WPF 原生处理
        if (_noActivateWindowController?.IsNoActivateSuspended == true)
        {
            return;
        }

        // 当按下 Ctrl/Alt/Win 等组合键时，优先放行，避免吃掉系统快捷键/全局热键（例如用于隐藏窗口的 Ctrl+`）
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
                _ = Application.Current.Dispatcher.InvokeAsync(() =>
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
                _ = Application.Current.Dispatcher.InvokeAsync(() => GetClipboardViewModel().SelectRelative(-1));
                e.Suppress = true;
                return;

            case VirtualKey.Down:
                if (hasDisallowedModifiers)
                {
                    return;
                }
                _ = Application.Current.Dispatcher.InvokeAsync(() => GetClipboardViewModel().SelectRelative(1));
                e.Suppress = true;
                return;

            case VirtualKey.Enter:
                if (hasDisallowedModifiers)
                {
                    return;
                }
                _ = Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var viewModel = GetClipboardViewModel();
                    if (viewModel.SelectedItem != null)
                    {
                        viewModel.PasteCommand.Execute(viewModel.SelectedItem);
                    }
                });
                e.Suppress = true;
                return;

            case VirtualKey.Backspace:
            case VirtualKey.Delete:
                if (hasDisallowedModifiers)
                {
                    return;
                }
                _ = Application.Current.Dispatcher.InvokeAsync(() => GetClipboardViewModel().BackspaceSearchText());
                e.Suppress = true;
                return;
        }

        if (TryGetPrintableText(e.Key, e.Modifiers, out var text))
        {
            if (hasDisallowedModifiers)
            {
                // 放行带修饰键的输入（例如 Ctrl+` 作为显示/隐藏快捷键）
                return;
            }

            _ = Application.Current.Dispatcher.InvokeAsync(() => GetClipboardViewModel().AppendSearchText(text));
            e.Suppress = true;
            return;
        }
    }

    private ClipMate.ViewModels.ClipboardViewModel GetClipboardViewModel()
    {
        return _clipboardViewModel ??= _container.Resolve<ClipMate.ViewModels.ClipboardViewModel>();
    }

    private static bool TryGetPrintableText(
        VirtualKey key,
        KeyModifiers modifiers,
        out string text)
    {
        var shift = modifiers.HasFlag(KeyModifiers.Shift);

        if (key is >= VirtualKey.A and <= VirtualKey.Z)
        {
            var c = (char)('a' + (key - VirtualKey.A));
            text = shift ? char.ToUpperInvariant(c).ToString() : c.ToString();
            return true;
        }

        if (key == VirtualKey.D0 || (key is >= VirtualKey.D1 and <= VirtualKey.D9))
        {
            var digit = key == VirtualKey.D0 ? '0' : (char)('1' + (key - VirtualKey.D1));
            text = digit.ToString();
            return true;
        }

        text = key switch
        {
            VirtualKey.Space => " ",
            VirtualKey.Comma => ",",
            VirtualKey.Period => ".",
            VirtualKey.Slash => "/",
            VirtualKey.Semicolon => ";",
            VirtualKey.Quote => "'",
            VirtualKey.Minus => "-",
            VirtualKey.Equals => "=",
            VirtualKey.LeftBracket => "[",
            VirtualKey.RightBracket => "]",
            VirtualKey.Backslash => "\\",
            VirtualKey.BackQuote => "`",
            _ => string.Empty
        };

        return text.Length > 0;
    }
}
