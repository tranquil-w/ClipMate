using ClipMate.Core.Models;
using ClipMate.Presentation.Clipboard;

namespace ClipMate.Services;

/// <summary>
/// 剪贴板展示/交互服务（Presentation 边界）
/// </summary>
public interface IClipboardService
{
    IClipboardContent Create(ClipboardItem item);
    IClipboardContent Create(object content);
    Task PasteAsync(IClipboardContent item);
}
