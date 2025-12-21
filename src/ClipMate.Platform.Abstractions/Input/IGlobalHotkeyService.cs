namespace ClipMate.Platform.Abstractions.Input;

public interface IGlobalHotkeyService : IDisposable
{
    event EventHandler<HotkeyEventArgs>? HotkeyPressed;

    bool Register(HotkeyDescriptor hotkey, Action callback);

    bool Unregister(HotkeyDescriptor hotkey);

    bool IsAvailable(HotkeyDescriptor hotkey);

    IReadOnlyCollection<HotkeyDescriptor> GetRegisteredHotkeys();

    void ClearAll();
}

