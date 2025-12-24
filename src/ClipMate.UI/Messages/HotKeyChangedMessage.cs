using CommunityToolkit.Mvvm.Messaging.Messages;

namespace ClipMate.Messages;

/// <summary>
/// 主快捷键设置变更消息
/// </summary>
/// <param name="newValue">新的快捷键字符串</param>
public class HotKeyChangedMessage(string newValue) : ValueChangedMessage<string>(newValue)
{
}

