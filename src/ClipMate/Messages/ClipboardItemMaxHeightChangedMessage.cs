using CommunityToolkit.Mvvm.Messaging.Messages;

namespace ClipMate.Messages;

/// <summary>
/// 剪贴项最大高度设置变更消息
/// </summary>
/// <remarks>
/// 初始化剪贴项最大高度设置变更消息
/// </remarks>
/// <param name="newValue">新的高度值</param>
public class ClipboardItemMaxHeightChangedMessage(int newValue) : ValueChangedMessage<int>(newValue)
{
}