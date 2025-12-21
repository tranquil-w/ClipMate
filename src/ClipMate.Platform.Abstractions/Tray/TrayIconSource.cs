namespace ClipMate.Platform.Abstractions.Tray;

public abstract record TrayIconSource
{
    private TrayIconSource()
    {
    }

    public sealed record ResourceUri(string Uri) : TrayIconSource;

    public sealed record FilePath(string Path) : TrayIconSource;

    public sealed record PngBytes(byte[] Bytes) : TrayIconSource;
}

