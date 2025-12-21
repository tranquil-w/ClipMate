namespace ClipMate.Platform.Abstractions.Input;

public sealed class KeyboardHookEventArgs : EventArgs
{
    public VirtualKey Key { get; }

    public KeyModifiers Modifiers { get; }

    /// <summary>
    /// 设为 true 表示拦截该按键事件，不继续向系统/前台应用传递。
    /// </summary>
    public bool Suppress { get; set; }

    public KeyboardHookEventArgs(VirtualKey key, KeyModifiers modifiers)
    {
        Key = key;
        Modifiers = modifiers;
    }
}

