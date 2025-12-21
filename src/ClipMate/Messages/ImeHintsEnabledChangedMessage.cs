using CommunityToolkit.Mvvm.Messaging.Messages;

namespace ClipMate.Messages;

/// <summary>
/// IME 降级提示开关变更消息
/// </summary>
/// <param name="newValue">是否启用 IME 提示</param>
public class ImeHintsEnabledChangedMessage(bool newValue) : ValueChangedMessage<bool>(newValue)
{
}

