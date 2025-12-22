namespace ClipMate.Service.Tests.Architecture;

public sealed class PresentationBoundaryTests
{
    [Fact]
    public void PresentationSource_ShouldNotReference_SystemWindowsClipboard()
    {
        var repoRoot = FindRepoRoot();
        var presentationRoot = Path.Combine(repoRoot, "src", "ClipMate");

        Assert.True(Directory.Exists(presentationRoot), $"未找到目录: {presentationRoot}");

        var offenders = Directory.EnumerateFiles(presentationRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => ContainsForbiddenClipboardReference(file))
            .Select(file => Path.GetRelativePath(repoRoot, file))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.True(offenders.Length == 0,
            "Presentation 层不应直接引用 System.Windows.Clipboard；请改用 IClipboardWriter/IClipboardService。\n" +
            string.Join("\n", offenders));
    }

    private static bool ContainsForbiddenClipboardReference(string filePath)
    {
        foreach (var line in File.ReadLines(filePath))
        {
            if (line.Contains("System.Windows.Clipboard", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ClipMate.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("无法定位仓库根目录（缺少 ClipMate.slnx）。");
    }
}

