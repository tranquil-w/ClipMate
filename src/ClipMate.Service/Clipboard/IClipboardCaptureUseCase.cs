using ClipMate.Platform.Abstractions.Clipboard;

namespace ClipMate.Service.Clipboard;

public interface IClipboardCaptureUseCase
{
    Task<ClipboardCaptureResult> CaptureAsync(ClipboardPayload payload, CancellationToken cancellationToken = default);
}

