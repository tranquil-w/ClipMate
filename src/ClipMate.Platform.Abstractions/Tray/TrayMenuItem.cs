namespace ClipMate.Platform.Abstractions.Tray;

public sealed record TrayMenuItem
{
    public static TrayMenuItem Separator { get; } = new TrayMenuItem
    {
        IsSeparator = true
    };

    public string? Title { get; init; }

    public bool IsSeparator { get; init; }

    public bool IsEnabled { get; init; } = true;

    public Action? OnClick { get; init; }

    public IReadOnlyList<TrayMenuItem>? Children { get; init; }
}

