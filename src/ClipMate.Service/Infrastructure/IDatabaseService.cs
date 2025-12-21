using ClipMate.Core.Models;

namespace ClipMate.Service.Infrastructure;

/// <summary>
/// 定义剪贴板记录的操作接口。
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// 初始化数据库（建表/建索引/必要时重建），该操作应幂等且可重复调用。
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有剪贴板记录，按时间倒序。
    /// </summary>
    Task<IEnumerable<ClipboardItem>> GetAllItemsDescAsync();

    /// <summary>
    /// 分页获取剪贴板记录，按时间倒序。
    /// </summary>
    Task<IReadOnlyList<ClipboardItem>> GetItemsPagedAsync(int offset, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过ID获取指定的剪贴板记录。
    /// </summary>
    Task<ClipboardItem?> GetItemAsync(int id);

    /// <summary>
    /// 插入一条新的剪贴板记录。
    /// </summary>
    Task<int> InsertItemAsync(ClipboardItem item);

    /// <summary>
    /// 更新现有的剪贴板记录。
    /// </summary>
    Task<bool> UpdateItemAsync(ClipboardItem item);

    /// <summary>
    /// 删除指定的剪贴板记录。
    /// </summary>
    Task<bool> DeleteItemAsync(ClipboardItem item);

    /// <summary>
    /// 更新指定剪贴板记录的收藏状态。
    /// </summary>
    Task<bool> UpdateFavoriteAsync(int id, bool isFavorite);

    /// <summary>
    /// 清理超出上限的历史记录（保留收藏项）。
    /// </summary>
    Task<int> CleanupOldItemsAsync(int limit);
}

