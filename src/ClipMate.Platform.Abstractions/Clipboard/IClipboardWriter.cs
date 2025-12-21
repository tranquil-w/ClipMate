namespace ClipMate.Platform.Abstractions.Clipboard;

public interface IClipboardWriter
{
    Task<bool> TrySetAsync(ClipboardPayload payload, CancellationToken cancellationToken = default);
}

