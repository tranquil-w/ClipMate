namespace ClipMate.Infrastructure;

/// <summary>
/// 版本信息提供器，用于动态获取程序集版本信息
/// </summary>
internal static class AppVersionProvider
{
    /// <summary>
    /// 获取程序集版本号
    /// </summary>
    /// <returns>版本号字符串，如 "0.0.4"</returns>
    internal static string GetCurrentVersion()
    {
        return System.Reflection.Assembly
            .GetExecutingAssembly()
            .GetName()
            .Version?.ToString() ?? "未知版本";
    }
}
