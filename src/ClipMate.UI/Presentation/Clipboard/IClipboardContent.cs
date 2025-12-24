using ClipMate.Core.Models;
using ClipMate.Core.Search;

namespace ClipMate.Presentation.Clipboard;

/// <summary>
/// 剪贴板内容接口，定义剪贴板项的基本操作
/// </summary>
public interface IClipboardContent
{
    /// <summary>
    /// 获取底层剪贴板数据项
    /// </summary>
    ClipboardItem Value { get; }

    /// <summary>
    /// 获取内容摘要，用于显示
    /// </summary>
    string Summary { get; }

    /// <summary>
    /// 获取或设置收藏状态
    /// </summary>
    bool IsFavorite { get; set; }

    /// <summary>
    /// 将内容复制到系统剪贴板
    /// </summary>
    Task CopyAsync();

    /// <summary>
    /// 判断内容是否符合搜索条件
    /// </summary>
    /// <param name="query">搜索查询快照</param>
    bool IsVisible(SearchQuerySnapshot query);
}
