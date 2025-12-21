using ClipMate.Core.Models;

namespace ClipMate.Service.Clipboard;

public interface IClipboardPasteUseCase
{
    Task PasteAsync(ClipboardItem item, CancellationToken cancellationToken = default);
}

