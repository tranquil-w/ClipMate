using ClipMate.Core.Search;
using ClipMate.Platform.Abstractions.Clipboard;
using ClipMate.Service.Clipboard;
using System.Reflection;

namespace ClipMate.Service.Tests.Architecture;

public class LayeringReferenceTests
{
    [Fact]
    public void Core_ShouldNotReference_WpfOrDatabaseAssemblies()
    {
        var referenced = GetReferencedAssemblyNames(typeof(SearchQuerySnapshot).Assembly);

        Assert.DoesNotContain("PresentationCore", referenced);
        Assert.DoesNotContain("PresentationFramework", referenced);
        Assert.DoesNotContain("WindowsBase", referenced);
        Assert.DoesNotContain("Microsoft.Data.Sqlite", referenced);
        Assert.DoesNotContain("Dapper", referenced);
    }

    [Fact]
    public void PlatformAbstractions_ShouldNotReference_WpfAssemblies()
    {
        var referenced = GetReferencedAssemblyNames(typeof(ClipboardPayload).Assembly);

        Assert.DoesNotContain("PresentationCore", referenced);
        Assert.DoesNotContain("PresentationFramework", referenced);
        Assert.DoesNotContain("WindowsBase", referenced);
    }

    [Fact]
    public void Service_ShouldNotReference_WpfOrPlatformWindowsAssemblies()
    {
        var referenced = GetReferencedAssemblyNames(typeof(ClipboardCaptureUseCase).Assembly);

        Assert.DoesNotContain("PresentationCore", referenced);
        Assert.DoesNotContain("PresentationFramework", referenced);
        Assert.DoesNotContain("WindowsBase", referenced);
        Assert.DoesNotContain("ClipMate.Platform.Windows", referenced);
    }

    [Fact]
    public void Service_ShouldNotReference_PresentationAssembly()
    {
        var referenced = GetReferencedAssemblyNames(typeof(ClipboardCaptureUseCase).Assembly);

        Assert.DoesNotContain("ClipMate", referenced);
    }

    private static string[] GetReferencedAssemblyNames(Assembly assembly)
    {
        return assembly.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();
    }
}
