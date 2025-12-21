using ClipMate.Platform.Abstractions.Input;
using NHotkey.Wpf;
using Serilog;
using System.Windows.Input;

namespace ClipMate.Platform.Windows.Input;

public sealed class WindowsGlobalHotkeyService : IGlobalHotkeyService
{
    private readonly ILogger _logger;
    private readonly Dictionary<HotkeyDescriptor, string> _registered = new();
    private bool _disposed;

    public event EventHandler<HotkeyEventArgs>? HotkeyPressed;

    public WindowsGlobalHotkeyService(ILogger logger)
    {
        _logger = logger;
    }

    public bool Register(HotkeyDescriptor hotkey, Action callback)
    {
        if (_disposed)
        {
            return false;
        }

        if (callback == null)
        {
            return false;
        }

        if (!KeyConversion.TryToWpfKey(hotkey.Key, out var key))
        {
            _logger.Warning("无法将 VirtualKey 转换为 WPF Key：{Key}", hotkey.Key);
            return false;
        }

        var modifiers = KeyConversion.ToWpfModifiers(hotkey.Modifiers);

        try
        {
            if (_registered.TryGetValue(hotkey, out _))
            {
                Unregister(hotkey);
            }

            var hotKeyId = $"ClipMate_{Guid.NewGuid():N}";

            HotkeyManager.Current.AddOrReplace(hotKeyId, key, modifiers, (_, e) =>
            {
                try
                {
                    HotkeyPressed?.Invoke(this, new HotkeyEventArgs(hotkey));
                    callback.Invoke();
                    e.Handled = true;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "执行全局快捷键回调失败：{Hotkey}", hotkey.DisplayString);
                }
            });

            _registered[hotkey] = hotKeyId;
            _logger.Information("全局快捷键注册成功：{Hotkey}", hotkey.DisplayString);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "注册全局快捷键失败：{Hotkey}", hotkey.DisplayString);
            return false;
        }
    }

    public bool Unregister(HotkeyDescriptor hotkey)
    {
        if (_disposed)
        {
            return false;
        }

        if (!_registered.TryGetValue(hotkey, out var id))
        {
            return false;
        }

        try
        {
            HotkeyManager.Current.Remove(id);
            _registered.Remove(hotkey);
            _logger.Information("全局快捷键取消注册成功：{Hotkey}", hotkey.DisplayString);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "取消注册全局快捷键失败：{Hotkey}", hotkey.DisplayString);
            return false;
        }
    }

    public bool IsAvailable(HotkeyDescriptor hotkey)
    {
        if (_disposed)
        {
            return false;
        }

        if (_registered.ContainsKey(hotkey))
        {
            return false;
        }

        if (!KeyConversion.TryToWpfKey(hotkey.Key, out var key))
        {
            return false;
        }

        var modifiers = KeyConversion.ToWpfModifiers(hotkey.Modifiers);
        var tempId = $"ClipMate_temp_{Guid.NewGuid():N}";

        try
        {
            HotkeyManager.Current.AddOrReplace(tempId, key, modifiers, (_, _) => { });
            HotkeyManager.Current.Remove(tempId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "检查全局快捷键可用性时失败：{Hotkey}", hotkey.DisplayString);
            try
            {
                HotkeyManager.Current.Remove(tempId);
            }
            catch
            {
                // ignore
            }
            return false;
        }
    }

    public IReadOnlyCollection<HotkeyDescriptor> GetRegisteredHotkeys()
    {
        return _registered.Keys.ToList();
    }

    public void ClearAll()
    {
        if (_disposed)
        {
            return;
        }

        var hotkeys = _registered.Keys.ToList();
        foreach (var hotkey in hotkeys)
        {
            Unregister(hotkey);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        ClearAll();
    }
}

