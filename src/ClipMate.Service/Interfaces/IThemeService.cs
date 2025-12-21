namespace ClipMate.Service.Interfaces
{
    public interface IThemeService
    {
        /// <summary>
        /// 获取当前主题设置
        /// </summary>
        string GetCurrentTheme();

        /// <summary>
        /// 应用指定的主题并保存到配置
        /// </summary>
        /// <param name="theme">主题名称：Light、Dark 或 System</param>
        void ApplyTheme(string theme);

        /// <summary>
        /// 启动系统主题变化监听
        /// </summary>
        void StartSystemThemeMonitoring();

        /// <summary>
        /// 停止系统主题变化监听
        /// </summary>
        void StopSystemThemeMonitoring();
    }
}
