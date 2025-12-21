namespace ClipMate.Platform.Abstractions.Input;

public sealed class HotkeyEventArgs : EventArgs
{
    public HotkeyDescriptor Hotkey { get; }

    public HotkeyEventArgs(HotkeyDescriptor hotkey)
    {
        Hotkey = hotkey;
    }
}

