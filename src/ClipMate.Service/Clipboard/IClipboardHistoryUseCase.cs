using ClipMate.Core.Models;

namespace ClipMate.Service.Clipboard;

public interface IClipboardHistoryUseCase
{
    Task<IReadOnlyList<ClipboardItem>> GetAllDescAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClipboardItem>> GetPagedAsync(int offset, int limit, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ClipboardItem item, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(ClipboardItem item, CancellationToken cancellationToken = default);

    Task<bool> UpdateFavoriteAsync(int id, bool isFavorite, CancellationToken cancellationToken = default);

    Task<int> CleanupOldItemsAsync(int limit, CancellationToken cancellationToken = default);
}
