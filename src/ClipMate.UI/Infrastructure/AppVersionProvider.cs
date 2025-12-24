namespace ClipMate.Infrastructure;

/// <summary>
/// 版本信息提供器，用于动态获取程序集版本信息
/// </summary>
public static class AppVersionProvider
{
    /// <summary>
    /// 获取程序集版本号
    /// </summary>
    /// <returns>版本号字符串，如 "0.0.4"</returns>
    public static string GetCurrentVersion()
    {
        return System.Reflection.Assembly
            .GetExecutingAssembly()
            .GetName()
            .Version?.ToString(3) ?? "未知版本";
    }
}
