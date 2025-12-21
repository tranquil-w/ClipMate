using ClipMate.Platform.Abstractions.Clipboard;

namespace ClipMate.Tests.TestHelpers;

public sealed class FakeClipboardChangeSource : IClipboardChangeSource
{
    public event EventHandler<ClipboardPayloadChangedEventArgs>? ClipboardChanged;

    public void Start()
    {
    }

    public void Stop()
    {
    }

    public void Raise(ClipboardPayload payload)
    {
        ClipboardChanged?.Invoke(this, new ClipboardPayloadChangedEventArgs(payload));
    }
}

