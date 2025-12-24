using ClipMate.Infrastructure;
using System.IO;

namespace ClipMate.UI.Bootstrap;

public static class AppDataPathProvider
{
    public static string GetAppDataFolder(string? appName = null)
    {
        var resolvedName = string.IsNullOrWhiteSpace(appName) ? Constants.AppName : appName;
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, resolvedName);
        Directory.CreateDirectory(appFolder);
        return appFolder;
    }
}
