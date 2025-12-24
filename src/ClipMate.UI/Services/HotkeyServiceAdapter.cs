using ClipMate.Service.Interfaces;
using ClipMate.Platform.Abstractions.Input;
using Serilog;

namespace ClipMate.Services;

public sealed class HotkeyServiceAdapter : IHotkeyService
{
    private readonly ILogger _logger;
    private readonly ISettingsService _settingsService;
    private readonly IGlobalHotkeyService _globalHotkeyService;

    public event EventHandler<string>? HotKeyPressed;

    public HotkeyServiceAdapter(
        ILogger logger,
        ISettingsService settingsService,
        IGlobalHotkeyService globalHotkeyService)
    {
        _logger = logger;
        _settingsService = settingsService;
        _globalHotkeyService = globalHotkeyService;

        _globalHotkeyService.HotkeyPressed += (_, e) =>
        {
            HotKeyPressed?.Invoke(this, e.Hotkey.DisplayString);
        };
    }

    public bool RegisterHotKey(string hotKey, Action callback)
    {
        if (!HotkeyDescriptor.TryParse(hotKey, out var descriptor))
        {
            return false;
        }

        return _globalHotkeyService.Register(descriptor.Value, callback);
    }

    public bool UnregisterHotKey(string hotKey)
    {
        if (!HotkeyDescriptor.TryParse(hotKey, out var descriptor))
        {
            return false;
        }

        return _globalHotkeyService.Unregister(descriptor.Value);
    }

    public bool IsHotKeyAvailable(string hotKey)
    {
        if (!HotkeyDescriptor.TryParse(hotKey, out var descriptor))
        {
            return false;
        }

        return _globalHotkeyService.IsAvailable(descriptor.Value);
    }

    public IEnumerable<string> GetRegisteredHotKeys()
    {
        return _globalHotkeyService.GetRegisteredHotkeys().Select(x => x.DisplayString);
    }

    public void ClearAllHotKeys()
    {
        _globalHotkeyService.ClearAll();
    }

    public bool RegisterMainWindowToggleHotkey(Action toggleCallback)
    {
        try
        {
            if (toggleCallback == null)
            {
                _logger.Warning("注册主窗口切换快捷键: 回调方法为空");
                return false;
            }

            var hotKey = _settingsService.GetHotKey();
            if (string.IsNullOrWhiteSpace(hotKey) || hotKey == "未设置")
            {
                hotKey = "Ctrl + `";
            }

            if (RegisterHotKey(hotKey, toggleCallback))
            {
                _logger.Information("主窗口切换快捷键注册成功: {Hotkey}", hotKey);
                return true;
            }

            if (!string.Equals(hotKey, "Ctrl + `", StringComparison.OrdinalIgnoreCase) &&
                RegisterHotKey("Ctrl + `", toggleCallback))
            {
                _logger.Warning("快捷键 {Hotkey} 注册失败，回退到默认 Ctrl + `", hotKey);
                return true;
            }

            _logger.Warning("主窗口切换快捷键注册失败: {Hotkey}", hotKey);
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "注册主窗口切换快捷键时发生错误");
            return false;
        }
    }
}

