namespace ClipMate.Platform.Abstractions.Clipboard;

public sealed class ClipboardPayloadChangedEventArgs(ClipboardPayload payload) : EventArgs
{
    public ClipboardPayload Payload { get; } = payload;
}

