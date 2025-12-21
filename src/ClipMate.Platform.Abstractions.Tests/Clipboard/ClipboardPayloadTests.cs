using ClipMate.Platform.Abstractions.Clipboard;
using System.Text.Json;

namespace ClipMate.Platform.Abstractions.Tests.Clipboard;

public class ClipboardPayloadTests
{
    [Fact]
    public void ClipboardPayload_ShouldRoundTripJson()
    {
        var payload = new ClipboardPayload(
            ClipboardPayloadType.ImagePng,
            Text: null,
            ImagePngBytes: new byte[] { 1, 2, 3 },
            FilePaths: null);

        var json = JsonSerializer.Serialize(payload);
        var restored = JsonSerializer.Deserialize<ClipboardPayload>(json);

        Assert.NotNull(restored);
        Assert.Equal(payload.Type, restored!.Type);
        Assert.Equal(payload.ImagePngBytes, restored.ImagePngBytes);
    }

    [Fact]
    public void PlatformAbstractions_ShouldNotReferenceWindowsAssemblies()
    {
        var referenced = typeof(IClipboardChangeSource).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("PresentationFramework", referenced);
        Assert.DoesNotContain("WindowsBase", referenced);
        Assert.DoesNotContain("System.Windows", referenced);
    }
}

