using ClipMate.Core.Models;

namespace ClipMate.Service.Clipboard;

public sealed class ClipboardHistoryUseCase(IClipboardItemRepository repository) : IClipboardHistoryUseCase
{
    private readonly IClipboardItemRepository _repository = repository;

    public Task<IReadOnlyList<ClipboardItem>> GetAllDescAsync(CancellationToken cancellationToken = default)
    {
        return _repository.GetAllDescAsync(cancellationToken);
    }

    public Task<IReadOnlyList<ClipboardItem>> GetPagedAsync(int offset, int limit, CancellationToken cancellationToken = default)
    {
        return _repository.GetPagedAsync(offset, limit, cancellationToken);
    }

    public Task<bool> UpdateAsync(ClipboardItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _repository.UpdateAsync(item, cancellationToken);
    }

    public Task<bool> DeleteAsync(ClipboardItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _repository.DeleteAsync(item, cancellationToken);
    }

    public Task<bool> UpdateFavoriteAsync(int id, bool isFavorite, CancellationToken cancellationToken = default)
    {
        return _repository.UpdateFavoriteAsync(id, isFavorite, cancellationToken);
    }

    public Task<int> CleanupOldItemsAsync(int limit, CancellationToken cancellationToken = default)
    {
        return _repository.CleanupOldItemsAsync(limit, cancellationToken);
    }
}
