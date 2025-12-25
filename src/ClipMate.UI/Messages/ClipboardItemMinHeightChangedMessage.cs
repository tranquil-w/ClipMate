using CommunityToolkit.Mvvm.Messaging.Messages;

namespace ClipMate.Messages;

/// <summary>
/// 剪贴项最小高度设置变更消息
/// </summary>
/// <remarks>
/// 初始化剪贴项最小高度设置变更消息
/// </remarks>
/// <param name="newValue">新的高度值</param>
public class ClipboardItemMinHeightChangedMessage(int newValue) : ValueChangedMessage<int>(newValue)
{
}
