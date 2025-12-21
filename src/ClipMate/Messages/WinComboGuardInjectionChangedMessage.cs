using CommunityToolkit.Mvvm.Messaging.Messages;

namespace ClipMate.Messages;

/// <summary>
/// Win 组合键保护注入开关变更消息
/// </summary>
/// <param name="newValue">是否启用保护注入</param>
public class WinComboGuardInjectionChangedMessage(bool newValue) : ValueChangedMessage<bool>(newValue)
{
}

