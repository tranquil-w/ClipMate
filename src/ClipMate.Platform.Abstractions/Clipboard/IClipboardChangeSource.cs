namespace ClipMate.Platform.Abstractions.Clipboard;

public interface IClipboardChangeSource
{
    event EventHandler<ClipboardPayloadChangedEventArgs> ClipboardChanged;

    void Start();

    void Stop();
}

