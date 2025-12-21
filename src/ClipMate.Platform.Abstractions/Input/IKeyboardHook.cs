namespace ClipMate.Platform.Abstractions.Input;

/// <summary>
/// 低级键盘钩子接口，用于拦截系统保留组合键（如 Win+V）以及无焦点覆盖层的键盘交互。
/// </summary>
public interface IKeyboardHook : IDisposable
{
    event EventHandler<KeyboardHookEventArgs>? KeyPressed;

    void Start();

    void Stop();
}

