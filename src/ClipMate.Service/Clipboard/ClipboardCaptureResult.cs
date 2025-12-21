using ClipMate.Core.Models;

namespace ClipMate.Service.Clipboard;

public sealed record ClipboardCaptureResult(int Id, ClipboardItem? Item)
{
    public static ClipboardCaptureResult Duplicate { get; } = new(-1, null);
}

