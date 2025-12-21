using ClipMate.Core.Models;
using Serilog.Events;

namespace ClipMate.Service.Interfaces
{
    /// <summary>
    /// 设置服务接口，用于管理应用程序配置
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>
        /// 获取快捷键设置
        /// </summary>
        string? GetHotKey();

        /// <summary>
        /// 设置快捷键
        /// </summary>
        void SetHotKey(string hotKey);

        /// <summary>
        /// 获取收藏筛选快捷键设置
        /// </summary>
        string? GetFavoriteFilterHotKey();

        /// <summary>
        /// 设置收藏筛选快捷键
        /// </summary>
        void SetFavoriteFilterHotKey(string hotKey);

        /// <summary>
        /// 获取主题设置
        /// </summary>
        string GetTheme();

        /// <summary>
        /// 设置主题
        /// </summary>
        void SetTheme(string theme);

        /// <summary>
        /// 获取日志级别
        /// </summary>
        LogEventLevel GetLogLevel();

        /// <summary>
        /// 设置日志级别
        /// </summary>
        void SetLogLevel(LogEventLevel level);

        /// <summary>
        /// 获取自动启动设置
        /// </summary>
        bool GetAutoStart();

        /// <summary>
        /// 设置自动启动
        /// </summary>
        void SetAutoStart(bool enabled);

        /// <summary>
        /// 获取静默启动设置
        /// </summary>
        bool GetSilentStart();

        /// <summary>
        /// 设置静默启动
        /// </summary>
        void SetSilentStart(bool value);

        /// <summary>
        /// 获取是否始终以管理员身份运行
        /// </summary>
        bool GetAlwaysRunAsAdmin();

        /// <summary>
        /// 设置是否始终以管理员身份运行
        /// </summary>
        void SetAlwaysRunAsAdmin(bool value);

        /// <summary>
        /// 获取剪贴板历史记录数量限制
        /// </summary>
        int GetHistoryLimit();

        /// <summary>
        /// 设置剪贴板历史记录数量限制
        /// </summary>
        void SetHistoryLimit(int limit);

        /// <summary>
        /// 获取剪贴项最大高度
        /// </summary>
        int GetClipboardItemMaxHeight();

        /// <summary>
        /// 设置剪贴项最大高度
        /// </summary>
        void SetClipboardItemMaxHeight(int height);

        /// <summary>
        /// 获取窗口显示位置
        /// </summary>
        WindowPosition GetWindowPosition();

        /// <summary>
        /// 设置窗口显示位置
        /// </summary>
        void SetWindowPosition(WindowPosition position);

        /// <summary>
        /// 获取是否启用 Win 组合键保护注入（用于 Win+V 拦截防止开始菜单误触发）
        /// </summary>
        bool GetEnableWinComboGuardInjection();

        /// <summary>
        /// 设置是否启用 Win 组合键保护注入
        /// </summary>
        void SetEnableWinComboGuardInjection(bool enabled);

        /// <summary>
        /// 获取是否启用 IME 降级提示
        /// </summary>
        bool GetImeHintsEnabled();

        /// <summary>
        /// 设置是否启用 IME 降级提示
        /// </summary>
        void SetImeHintsEnabled(bool enabled);

        /// <summary>
        /// 保存所有设置
        /// </summary>
        Task SaveAsync();

        /// <summary>
        /// 加载所有设置
        /// </summary>
        Task LoadAsync();

        /// <summary>
        /// 获取用户文件夹路径
        /// </summary>
        string GetUserFolder();
    }
}
