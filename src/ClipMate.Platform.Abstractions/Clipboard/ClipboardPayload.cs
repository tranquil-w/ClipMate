namespace ClipMate.Platform.Abstractions.Clipboard;

public sealed record ClipboardPayload(
    ClipboardPayloadType Type,
    string? Text = null,
    byte[]? ImagePngBytes = null,
    IReadOnlyList<string>? FilePaths = null);

