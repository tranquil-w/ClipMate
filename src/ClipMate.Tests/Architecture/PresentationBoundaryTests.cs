using ClipMate.ViewModels;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace ClipMate.Tests.Architecture;

public sealed class PresentationBoundaryTests
{
    [Fact]
    public void Presentation_ShouldNotReference_SystemWindowsClipboard()
    {
        var assembly = typeof(ClipboardViewModel).Assembly;
        var assemblyPath = assembly.Location;

        Assert.False(string.IsNullOrWhiteSpace(assemblyPath));
        Assert.True(File.Exists(assemblyPath));

        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        var reader = peReader.GetMetadataReader();

        var referencesClipboard = reader.TypeReferences
            .Select(handle => reader.GetTypeReference(handle))
            .Any(typeRef =>
                reader.GetString(typeRef.Namespace).Equals("System.Windows", StringComparison.Ordinal) &&
                reader.GetString(typeRef.Name).Equals("Clipboard", StringComparison.Ordinal));

        Assert.False(referencesClipboard);
    }
}

