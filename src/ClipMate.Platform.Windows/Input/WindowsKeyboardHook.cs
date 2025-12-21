using ClipMate.Platform.Abstractions.Input;
using ClipMate.Platform.Windows.Interop;
using Serilog;
using SharpHook;
using SharpHook.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HookEventArgs = SharpHook.KeyboardHookEventArgs;

namespace ClipMate.Platform.Windows.Input;

public sealed class WindowsKeyboardHook : IKeyboardHook
{
    private readonly ILogger _logger;
    private readonly Func<IGlobalHook> _hookFactory;
    private readonly Action _injectWinComboGuard;
    private IGlobalHook? _hook;
    private bool _disposed;

    private bool _isWinPressed;
    private bool _isCtrlPressed;
    private bool _isAltPressed;
    private bool _isShiftPressed;

    // 对称抑制：如果某个按键的 KeyDown 被 Suppress，则该按键的 KeyUp 也必须被 Suppress
    private readonly HashSet<KeyCode> _suppressedPhysicalKeys = new();

    // Win 组合键保护注入状态机（用于 Win 组合键拦截场景）
    private bool _winComboSuppressedActive;
    private bool _winComboGuardInjected;
    private DateTimeOffset? _winComboGuardInjectedAt;

    /// <summary>
    /// 是否允许平台层为 Win+V 拦截注入保护 tap（RightShift）。
    /// 该开关由应用层根据设置决定，默认启用。
    /// </summary>
    public bool EnableWinComboGuardInjection { get; set; } = true;

    public event EventHandler<ClipMate.Platform.Abstractions.Input.KeyboardHookEventArgs>? KeyPressed;

    public WindowsKeyboardHook(
        ILogger logger,
        Func<IGlobalHook>? hookFactory = null,
        Action? injectWinComboGuard = null)
    {
        _logger = logger;
        _hookFactory = hookFactory ?? (() => new SimpleGlobalHook(GlobalHookType.Keyboard, null, runAsyncOnBackgroundThread: true));
        _injectWinComboGuard = injectWinComboGuard ?? KeyboardInput.SendRightShiftTapBestEffort;
    }

    public void Start()
    {
        if (_disposed)
        {
            return;
        }

        if (_hook != null)
        {
            return;
        }

        try
        {
            _hook = _hookFactory();
            _hook.KeyPressed += OnKeyPressed;
            _hook.KeyReleased += OnKeyReleased;
            _ = _hook.RunAsync();
            _logger.Information("WindowsKeyboardHook 已启动");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "启动 WindowsKeyboardHook 失败");
            Stop();
        }
    }

    public void Stop()
    {
        if (_hook == null)
        {
            return;
        }

        try
        {
            _hook.KeyPressed -= OnKeyPressed;
            _hook.KeyReleased -= OnKeyReleased;
            _hook.Dispose();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "停止 WindowsKeyboardHook 失败");
        }
        finally
        {
            _hook = null;
            _isWinPressed = false;
            _isCtrlPressed = false;
            _isAltPressed = false;
            _isShiftPressed = false;
            _suppressedPhysicalKeys.Clear();
            _winComboSuppressedActive = false;
            _winComboGuardInjected = false;
            _winComboGuardInjectedAt = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
    }

    private void OnKeyPressed(object? sender, HookEventArgs e)
    {
        switch (e.Data.KeyCode)
        {
            case KeyCode.VcLeftMeta:
            case KeyCode.VcRightMeta:
                _isWinPressed = true;
                return;
            case KeyCode.VcLeftControl:
            case KeyCode.VcRightControl:
                _isCtrlPressed = true;
                return;
            case KeyCode.VcLeftAlt:
            case KeyCode.VcRightAlt:
                _isAltPressed = true;
                return;
            case KeyCode.VcLeftShift:
            case KeyCode.VcRightShift:
                _isShiftPressed = true;
                return;
        }

        if (!TryMapKeyCodeToVirtualKey(e.Data.KeyCode, out var key))
        {
            return;
        }

        var modifiers = KeyModifiers.None;
        if (_isCtrlPressed) modifiers |= KeyModifiers.Ctrl;
        if (_isAltPressed) modifiers |= KeyModifiers.Alt;
        if (_isShiftPressed) modifiers |= KeyModifiers.Shift;
        if (_isWinPressed) modifiers |= KeyModifiers.Win;

        var args = new ClipMate.Platform.Abstractions.Input.KeyboardHookEventArgs(key, modifiers);
        try
        {
            KeyPressed?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "处理键盘钩子 KeyPressed 事件失败，KeyCode: {KeyCode}", e.Data.KeyCode);
        }

        if (args.Suppress)
        {
            e.SuppressEvent = true;
            _suppressedPhysicalKeys.Add(e.Data.KeyCode);

            // 当 Win 组合键被应用层明确 Suppress 时进入 Win 组合键保护逻辑
            if (_isWinPressed)
            {
                _winComboSuppressedActive = true;
            }
        }
    }

    private void OnKeyReleased(object? sender, HookEventArgs e)
    {
        // 对称抑制：确保 KeyUp 不会单独泄漏到系统/前台应用
        var wasSuppressed = _suppressedPhysicalKeys.Remove(e.Data.KeyCode);
        if (wasSuppressed)
        {
            e.SuppressEvent = true;
        }

        if (wasSuppressed && _winComboSuppressedActive && _isWinPressed)
        {
            InjectWinComboGuardIfNeeded("ComboKeyReleased");
        }

        switch (e.Data.KeyCode)
        {
            case KeyCode.VcLeftMeta:
            case KeyCode.VcRightMeta:
                // 触发条件：Win 组合键被 Suppress 后释放 Win，系统可能走 Win 单键路径
                if (_winComboSuppressedActive)
                {
                    InjectWinComboGuardIfNeeded("WinReleased");
                }
                _isWinPressed = false;
                _winComboSuppressedActive = false;
                _winComboGuardInjected = false;
                _winComboGuardInjectedAt = null;
                return;
            case KeyCode.VcLeftControl:
            case KeyCode.VcRightControl:
                _isCtrlPressed = false;
                return;
            case KeyCode.VcLeftAlt:
            case KeyCode.VcRightAlt:
                _isAltPressed = false;
                return;
            case KeyCode.VcLeftShift:
            case KeyCode.VcRightShift:
                _isShiftPressed = false;
                return;
        }
    }

    private void InjectWinComboGuardIfNeeded(string reason)
    {
        if (_winComboGuardInjected)
        {
            return;
        }

        _winComboGuardInjected = true;
        _winComboGuardInjectedAt = DateTimeOffset.UtcNow;

        if (!EnableWinComboGuardInjection)
        {
            _logger.Debug("Win 组合键保护注入已禁用，跳过注入（Reason={Reason}）", reason);
            return;
        }

        try
        {
            _injectWinComboGuard();
            _logger.Debug("已注入 Win 组合键保护 tap（RightShift），Reason={Reason}", reason);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "注入 Win 组合键保护事件失败（Reason={Reason}）", reason);
            return;
        }

        // best-effort：如果系统策略/兼容性导致 tap 未完全释放，延迟补发 KeyUp，避免卡键
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(150)).ConfigureAwait(false);
                KeyboardInput.SendRightShiftKeyUpBestEffort();

                if (_winComboGuardInjectedAt is { } injectedAt &&
                    DateTimeOffset.UtcNow - injectedAt > TimeSpan.FromMilliseconds(100))
                {
                    _logger.Debug("Win 组合键保护 KeyUp 补偿已执行（DelayMs={DelayMs}）", 150);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Win 组合键保护 KeyUp 补偿失败");
            }
        });
    }

    private static bool TryMapKeyCodeToVirtualKey(KeyCode keyCode, out VirtualKey key)
    {
        key = VirtualKey.None;

        if (keyCode is >= KeyCode.VcA and <= KeyCode.VcZ)
        {
            key = VirtualKey.A + (keyCode - KeyCode.VcA);
            return true;
        }

        if (keyCode is >= KeyCode.Vc1 and <= KeyCode.Vc9)
        {
            key = VirtualKey.D1 + (keyCode - KeyCode.Vc1);
            return true;
        }

        if (keyCode == KeyCode.Vc0)
        {
            key = VirtualKey.D0;
            return true;
        }

        if (keyCode is >= KeyCode.VcF1 and <= KeyCode.VcF12)
        {
            key = VirtualKey.F1 + (keyCode - KeyCode.VcF1);
            return true;
        }

        key = keyCode switch
        {
            KeyCode.VcEnter => VirtualKey.Enter,
            KeyCode.VcEscape => VirtualKey.Escape,
            KeyCode.VcTab => VirtualKey.Tab,
            KeyCode.VcSpace => VirtualKey.Space,
            KeyCode.VcBackspace => VirtualKey.Backspace,
            KeyCode.VcDelete => VirtualKey.Delete,
            KeyCode.VcInsert => VirtualKey.Insert,
            KeyCode.VcHome => VirtualKey.Home,
            KeyCode.VcEnd => VirtualKey.End,
            KeyCode.VcPageUp => VirtualKey.PageUp,
            KeyCode.VcPageDown => VirtualKey.PageDown,
            KeyCode.VcUp => VirtualKey.Up,
            KeyCode.VcDown => VirtualKey.Down,
            KeyCode.VcLeft => VirtualKey.Left,
            KeyCode.VcRight => VirtualKey.Right,
            KeyCode.VcComma => VirtualKey.Comma,
            KeyCode.VcPeriod => VirtualKey.Period,
            KeyCode.VcSlash => VirtualKey.Slash,
            KeyCode.VcSemicolon => VirtualKey.Semicolon,
            KeyCode.VcQuote => VirtualKey.Quote,
            KeyCode.VcMinus => VirtualKey.Minus,
            KeyCode.VcEquals => VirtualKey.Equals,
            KeyCode.VcOpenBracket => VirtualKey.LeftBracket,
            KeyCode.VcCloseBracket => VirtualKey.RightBracket,
            KeyCode.VcBackslash => VirtualKey.Backslash,
            KeyCode.VcBackQuote => VirtualKey.BackQuote,
            _ => VirtualKey.None
        };

        return key != VirtualKey.None;
    }
}
