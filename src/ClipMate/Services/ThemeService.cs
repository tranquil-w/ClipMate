using ClipMate.Service.Interfaces;
using HandyControl.Data;
using Microsoft.Win32;
using Serilog;
using System.Windows;

namespace ClipMate.Services
{
    public class ThemeService(ISettingsService settingsService, ILogger logger) : IThemeService
    {
        private readonly ISettingsService _settingsService = settingsService;
        private readonly ILogger _logger = logger;
        private bool _isMonitoring = false;

        public string GetCurrentTheme()
        {
            return _settingsService.GetTheme();
        }

        private void ApplyThemeInternal(string theme)
        {
            _logger.Information("开始应用主题：{Theme}", theme);

            try
            {
                switch (theme)
                {
                    case "Light":
                        SetTheme(SkinType.Default);
                        _logger.Information("已应用浅色主题");
                        break;
                    case "Dark":
                        SetTheme(SkinType.Dark);
                        _logger.Information("已应用深色主题");
                        break;
                    case "System":
                    default:
                        var isDark = IsDarkTheme();
                        SetTheme(isDark ? SkinType.Dark : SkinType.Default);
                        _logger.Information("已应用系统主题（{SystemTheme}）", isDark ? "深色" : "浅色");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "应用主题 {Theme} 失败", theme);
                throw;
            }
        }

        /// <summary>
        /// 实际应用主题的逻辑
        /// </summary>
        /// <param name="themeType"></param>
        private void SetTheme(SkinType themeType)
        {
            try
            {
                _logger.Debug("加载主题资源：{ThemeType}", themeType);

                var md = Application.Current.Resources.MergedDictionaries;

                // 1. 先加载新的主题资源（确保资源始终可用，避免切换时资源找不到的警告）
                var skinResource = new ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/HandyControl;component/Themes/Skin{themeType}.xaml")
                };
                var themeResource = new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/HandyControl;component/Themes/Theme.xaml")
                };

                // 2. 添加新资源到开头
                md.Insert(0, skinResource);
                md.Insert(1, themeResource);
                _logger.Debug("主题资源已添加到资源字典");

                // 3. 移除旧的主题资源（索引从 2 开始，因为前面添加了两个新资源）
                int removedCount = 0;
                for (int i = 2; i < md.Count; i++)
                {
                    if (md[i].Source != null && (
                        md[i].Source.OriginalString.StartsWith("pack://application:,,,/HandyControl;component/Themes/Skin") ||
                        md[i].Source.OriginalString == "pack://application:,,,/HandyControl;component/Themes/Theme.xaml"))
                    {
                        md.RemoveAt(i--);
                        removedCount++;
                    }
                }

                if (removedCount > 0)
                {
                    _logger.Debug("已移除 {Count} 个旧主题资源", removedCount);
                }

                // 只有在主窗口已创建时才调用 OnApplyTemplate
                Application.Current.MainWindow?.OnApplyTemplate();
                _logger.Debug("主题资源应用完成");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载主题资源 {ThemeType} 失败", themeType);
                throw;
            }
        }

        /// <summary>
        /// 应用指定的主题并保存到配置
        /// </summary>
        /// <param name="theme">主题名称：Light、Dark 或 System</param>
        public void ApplyTheme(string theme)
        {
            // 立即应用主题
            ApplyThemeInternal(theme);

            // 注意：保存逻辑由 SettingsService 处理
            // ThemeService 不直接操作配置持久化，保持职责分离
        }

        /// <summary>
        /// 判断当前系统是否为暗色主题
        /// </summary>
        /// <returns></returns>
        private bool IsDarkTheme()
        {
            try
            {
                // 读取注册表，判断系统主题
                const string registryKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                const string registryValue = "AppsUseLightTheme";

                var theme = Registry.GetValue(registryKey, registryValue, null);

                // 0 表示暗色主题，1 表示浅色主题
                // 如果无法读取注册表，则假设为浅色主题
                var isDark = theme is 0;
                _logger.Debug("检测到系统主题：{SystemTheme}（注册表值：{RegistryValue}）",
                    isDark ? "深色" : "浅色", theme);
                return isDark;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "无法读取系统主题设置，使用默认浅色主题");
                return false;
            }
        }

        /// <summary>
        /// 启动系统主题变化监听
        /// </summary>
        public void StartSystemThemeMonitoring()
        {
            if (_isMonitoring)
            {
                _logger.Debug("系统主题监听已启动，跳过重复启动");
                return;
            }

            try
            {
                Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnSystemThemeChanged;
                _isMonitoring = true;
                _logger.Information("已启动系统主题变化监听");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "启动系统主题监听失败");
            }
        }

        /// <summary>
        /// 停止系统主题变化监听
        /// </summary>
        public void StopSystemThemeMonitoring()
        {
            if (!_isMonitoring)
            {
                return;
            }

            try
            {
                Microsoft.Win32.SystemEvents.UserPreferenceChanged -= OnSystemThemeChanged;
                _isMonitoring = false;
                _logger.Information("已停止系统主题变化监听");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "停止系统主题监听失败");
            }
        }

        /// <summary>
        /// 系统主题变化事件处理
        /// </summary>
        private void OnSystemThemeChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
        {
            try
            {
                // 只处理与常规首选项相关的更改（包括主题变化）
                if (e.Category != Microsoft.Win32.UserPreferenceCategory.General)
                {
                    return;
                }

                var currentTheme = GetCurrentTheme();
                _logger.Debug("检测到系统偏好设置变化，当前主题设置：{Theme}", currentTheme);

                // 只有当用户选择了"System"主题时才自动切换
                if (currentTheme == "System")
                {
                    _logger.Information("系统主题已变化，自动更新应用主题");

                    // 在UI线程上应用主题
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ApplyThemeInternal(currentTheme);
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "处理系统主题变化事件时发生错误");
            }
        }
    }
}
