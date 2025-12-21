using ClipMate.Core.Models;
using ClipMate.Service.Clipboard;

namespace ClipMate.Service.Infrastructure;

public sealed class DatabaseClipboardItemRepository(IDatabaseService databaseService) : IClipboardItemRepository
{
    private readonly IDatabaseService _databaseService = databaseService;

    public Task<int> InsertAsync(ClipboardItem item, CancellationToken cancellationToken = default)
    {
        return _databaseService.InsertItemAsync(item);
    }

    public async Task<IReadOnlyList<ClipboardItem>> GetAllDescAsync(CancellationToken cancellationToken = default)
    {
        var items = await _databaseService.GetAllItemsDescAsync();
        return items.ToArray();
    }

    public Task<IReadOnlyList<ClipboardItem>> GetPagedAsync(int offset, int limit, CancellationToken cancellationToken = default)
    {
        return _databaseService.GetItemsPagedAsync(offset, limit, cancellationToken);
    }

    public Task<bool> UpdateAsync(ClipboardItem item, CancellationToken cancellationToken = default)
    {
        return _databaseService.UpdateItemAsync(item);
    }

    public Task<bool> DeleteAsync(ClipboardItem item, CancellationToken cancellationToken = default)
    {
        return _databaseService.DeleteItemAsync(item);
    }

    public Task<bool> UpdateFavoriteAsync(int id, bool isFavorite, CancellationToken cancellationToken = default)
    {
        return _databaseService.UpdateFavoriteAsync(id, isFavorite);
    }

    public Task<int> CleanupOldItemsAsync(int limit, CancellationToken cancellationToken = default)
    {
        return _databaseService.CleanupOldItemsAsync(limit);
    }
}
