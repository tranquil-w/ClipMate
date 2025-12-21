using ClipMate.Service.Interfaces;

namespace ClipMate.Tests.TestHelpers;

public sealed class FakeHotkeyService : IHotkeyService
{
    public event EventHandler<string>? HotKeyPressed;

    public bool RegisterHotKey(string hotKey, Action callback) => true;

    public bool UnregisterHotKey(string hotKey) => true;

    public bool IsHotKeyAvailable(string hotKey) => true;

    public IEnumerable<string> GetRegisteredHotKeys() => Array.Empty<string>();

    public void ClearAllHotKeys()
    {
    }

    public bool RegisterMainWindowToggleHotkey(Action toggleCallback) => true;

    public void RaisePressed(string hotkey) => HotKeyPressed?.Invoke(this, hotkey);
}

