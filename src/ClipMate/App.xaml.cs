using ClipMate.Composition;
using ClipMate.Core.Models;
using ClipMate.Infrastructure;
using ClipMate.Service.Interfaces;
using ClipMate.Platform.Abstractions.Clipboard;
using ClipMate.Platform.Abstractions.Input;
using ClipMate.Platform.Abstractions.Window;
using ClipMate.Service.Infrastructure;
using ClipMate.Platform.Abstractions.Tray;
using ClipMate.Service.Clipboard;
using ClipMate.Services;
using ClipMate.Messages;
using ClipMate.ViewModels;
using ClipMate.Views;
using ClipMate.Platform.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Windows;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClipMate
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        private const string _mutexName = "ClipMate";
        private static Mutex? _mutex;
        private ITrayIcon? _trayIcon;
        private volatile bool _isMainWindowVisible;
        private volatile bool _isMainWindowActive;
        private readonly LoggingLevelSwitch _loggingLevelSwitch = new(LogEventLevel.Information);

        public static new App Current => (App)Application.Current;

        public IConfiguration? Configuration { get; private set; }

        private static string GetAppDataFolder()
        {
            // 使用 LocalApplicationData 确保数据存储在用户的本地应用数据文件夹中
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "ClipMate");

            // 确保文件夹存在
            Directory.CreateDirectory(appFolder);

            return appFolder;
        }

        protected override Window CreateShell()
        {
            var mainWindow = Container.Resolve<MainWindow>();
            mainWindow.DataContext = Container.Resolve<MainWindowViewModel>();
            return mainWindow;
        }

        protected override void InitializeShell(Window shell)
        {
            // 只设置 MainWindow 引用，不显示窗口
            // 注意：不能调用 base.InitializeShell(shell)，因为它会调用 shell.Show()
            // 手动设置 MainWindow 引用
            Application.Current.MainWindow = shell;
        }

        private static JsonSerializerOptions CreateSettingsJsonOptions()
        {
            return new JsonSerializerOptions
            {
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                Converters = { new LogEventLevelJsonConverter(), new JsonStringEnumConverter() }
            };
        }

        /// <summary>
        /// 通用的配置读取方法，从默认设置和用户设置中读取指定属性
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="propertySelector">属性选择器</param>
        /// <param name="defaultValue">默认值</param>
        /// <param name="errorMessage">错误日志消息</param>
        /// <returns>配置值</returns>
        private T ReadSetting<T>(Func<AppSettings, T?> propertySelector, T defaultValue, string errorMessage) where T : struct
        {
            try
            {
                var options = CreateSettingsJsonOptions();
                var defaultSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                var userSettingsPath = Path.Combine(GetAppDataFolder(), "settings.json");

                T? value = null;

                if (File.Exists(defaultSettingsPath))
                {
                    var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(defaultSettingsPath), options);
                    if (settings != null)
                        value = propertySelector(settings);
                }

                if (File.Exists(userSettingsPath))
                {
                    var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(userSettingsPath), options);
                    if (settings != null)
                        value = propertySelector(settings);
                }

                return value ?? defaultValue;
            }
            catch (Exception ex)
            {
                Log.Error(ex, errorMessage);
                return defaultValue;
            }
        }

        /// <summary>
        /// 直接读取 SilentStart 设置（不依赖 DI 容器）
        /// </summary>
        private bool ReadSilentStartSetting()
        {
            return ReadSetting(s => s.SilentStart, false, "读取静默启动配置时发生错误");
        }

        /// <summary>
        /// 读取日志级别设置
        /// </summary>
        private LogEventLevel ReadLogLevelSetting()
        {
            var level = ReadSetting(s => s.LogLevel, LogEventLevel.Information, "读取日志级别配置时发生错误");
            return LogLevelPolicy.Normalize(level);
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 构建 IConfiguration 对象，加载 appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var configuration = builder.Build();
            Configuration = configuration;

            // 获取用户应用数据文件夹路径
            var appDataFolder = GetAppDataFolder();

            // 注册日志
            var logFolder = Path.Combine(appDataFolder, "Logs");
            Directory.CreateDirectory(logFolder); // 确保日志文件夹存在

            _loggingLevelSwitch.MinimumLevel = ReadLogLevelSetting();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(_loggingLevelSwitch)
                .WriteTo.Debug()
                .WriteTo.File(Path.Combine(logFolder, "clipmate-.log"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            containerRegistry.RegisterInfrastructure(configuration, appDataFolder, _loggingLevelSwitch, Log.Logger);
            containerRegistry.RegisterAppServices();
            containerRegistry.RegisterPlatformWindows();
            containerRegistry.RegisterServiceLayer();
            containerRegistry.RegisterPresentation();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // 检查是否需要以管理员身份重启
            if (TryRestartAsAdminIfNeeded())
            {
                Shutdown();
                return;
            }

            // Check if another instance is already running
            _mutex = new Mutex(true, _mutexName, out var createdNew);
            if (!createdNew)
            {
                Shutdown();
                return;
            }

            // 初始化 ContextMenu 状态追踪器
            ContextMenuTracker.Initialize();

            base.OnStartup(e);

            // 在窗口创建之前应用主题，确保资源在 XAML 解析时可用
            var themeService = Container.Resolve<IThemeService>();
            themeService.ApplyTheme(themeService.GetCurrentTheme());

            // 启动系统主题监听
            themeService.StartSystemThemeMonitoring();
        }

        /// <summary>
        /// 检查是否需要以管理员身份重启
        /// </summary>
        private bool TryRestartAsAdminIfNeeded()
        {
            try
            {
                var adminService = new AdminService();
                if (adminService.IsRunningAsAdministrator())
                {
                    return false;
                }

                if (!ReadAlwaysRunAsAdminSetting())
                {
                    return false;
                }

                var restarted = adminService.RestartAsAdministrator();
                if (restarted)
                {
                    Log.Information("检测到已启用'始终以管理员身份运行'，正在以管理员权限重启应用");
                    return true;
                }

                Log.Information("管理员权限提升请求被取消");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "自动请求管理员权限时发生错误");
            }

            return false;
        }

        /// <summary>
        /// 读取 AlwaysRunAsAdmin 设置
        /// </summary>
        private bool ReadAlwaysRunAsAdminSetting()
        {
            return ReadSetting(s => s.AlwaysRunAsAdmin, false, "读取管理员运行配置时发生错误");
        }

        protected override void OnInitialized()
        {
            // 不调用 base.OnInitialized()，因为它会无条件调用 MainWindow.Show()
            // 根据静默启动设置决定是否显示窗口
            var silentStart = ReadSilentStartSetting();

            if (!silentStart)
            {
                if (MainWindow != null)
                {
                    NoActivateWindowController.ShowNoActivate(MainWindow);
                }
            }

            InitializeMainWindowStateTracking();

            // 初始化托盘图标（独立于主窗口）
            InitializeNotifyIcon();

            // 初始化后台服务（不依赖窗口显示）
            InitializeBackgroundServices();

            if (MainWindow != null)
            {
                Container.Resolve<MainWindowOverlayService>().Initialize(MainWindow);
            }
        }

        /// <summary>
        /// 初始化托盘图标（独立于主窗口，始终显示）
        /// </summary>
        private void InitializeNotifyIcon()
        {
            _trayIcon = Container.Resolve<ITrayIcon>();
            _trayIcon.ToolTip = "ClipMate";
            _trayIcon.SetIcon(new TrayIconSource.ResourceUri("pack://application:,,,/ClipMate;component/ClipMate.ico"));
            _trayIcon.SetContextMenu(
            [
                new TrayMenuItem
                {
                    Title = "用户文件夹",
                    OnClick = () => Container.Resolve<NotifyIconCommandHandler>().OpenUserFolder()
                },
                new TrayMenuItem
                {
                    Title = "设置",
                    OnClick = () => Container.Resolve<NotifyIconCommandHandler>().OpenSettings()
                },
                TrayMenuItem.Separator,
                new TrayMenuItem
                {
                    Title = "退出",
                    OnClick = () => Container.Resolve<IApplicationService>().Shutdown()
                }
            ]);

            _trayIcon.Show();
            _trayIcon.Clicked += (_, _) => _ = Application.Current.Dispatcher.InvokeAsync(ToggleMainWindow);
        }

        /// <summary>
        /// 初始化后台服务（剪贴板监听、键盘钩子、快捷键注册）
        /// </summary>
        private void InitializeBackgroundServices()
        {
            Log.Information("程序启动");

            _ = Task.Run(async () =>
            {
                try
                {
                    await Container.Resolve<IDatabaseService>().InitializeAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "数据库初始化失败");
                }
            });

            Container.Resolve<IClipboardChangeSource>().Start();
            Container.Resolve<IForegroundWindowTracker>().Start();

            // 启动键盘钩子（始终运行：拦截 Win+V + 无焦点覆盖层键盘交互 + 快捷键录制）
            var keyboardHook = Container.Resolve<IKeyboardHook>();
            keyboardHook.Start();

            // 注册快捷键
            var settingsService = Container.Resolve<ISettingsService>();        
            var hotkeyService = Container.Resolve<IHotkeyService>();
            var currentHotkey = settingsService.GetHotKey();

            if (keyboardHook is WindowsKeyboardHook windowsKeyboardHook)
            {
                windowsKeyboardHook.EnableWinComboGuardInjection = settingsService.GetEnableWinComboGuardInjection();
                WeakReferenceMessenger.Default.Register<WinComboGuardInjectionChangedMessage>(
                    this,
                    (_, message) => windowsKeyboardHook.EnableWinComboGuardInjection = message.Value);
            }

            // Win+V 通过键盘钩子拦截（避免系统剪贴板弹出）；当用户切换快捷键时无需重启即可生效
            var isWinVHotkeyCached = IsWinVHotkey(currentHotkey);
            WeakReferenceMessenger.Default.Register<HotKeyChangedMessage>(
                this,
                (_, message) => isWinVHotkeyCached = IsWinVHotkey(message.Value));

            HotkeyDescriptor? favoriteFilterHotkey = null;
            if (HotkeyDescriptor.TryParse(settingsService.GetFavoriteFilterHotKey(), out var favoriteDescriptor))
            {
                favoriteFilterHotkey = favoriteDescriptor;
            }

            WeakReferenceMessenger.Default.Register<FavoriteFilterHotKeyChangedMessage>(
                this,
                (_, message) =>
                {
                    favoriteFilterHotkey = HotkeyDescriptor.TryParse(message.Value, out var parsed)
                        ? parsed
                        : null;
                });
            keyboardHook.KeyPressed += (_, e) =>
            {
                if (!isWinVHotkeyCached)
                {
                    return;
                }

                if (!e.Modifiers.HasFlag(KeyModifiers.Win) || e.Key != VirtualKey.V)
                {
                    return;
                }

                e.Suppress = true;
                _ = Application.Current.Dispatcher.InvokeAsync(ToggleMainWindow);
            };

            keyboardHook.KeyPressed += (_, e) =>
            {
                if (!_isMainWindowVisible || _isMainWindowActive)
                {
                    return;
                }

                var descriptor = favoriteFilterHotkey;
                if (descriptor is null)
                {
                    return;
                }

                if (e.Key != descriptor.Value.Key || e.Modifiers != descriptor.Value.Modifiers)
                {
                    return;
                }

                e.Suppress = true;
                _ = Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    WeakReferenceMessenger.Default.Send(new FavoriteFilterHotKeyPressedMessage());
                });
            };

            if (!IsWinVHotkey(currentHotkey))
            {
                // 其他快捷键使用 NHotkey 处理
                hotkeyService.RegisterMainWindowToggleHotkey(ToggleMainWindow);
            }

            // 在后台清理超出上限的历史记录
            _ = Task.Run(async () =>
            {
                try
                {
                    var historyLimit = settingsService.GetHistoryLimit();
                    var historyUseCase = Container.Resolve<IClipboardHistoryUseCase>();
                    var deletedCount = await historyUseCase.CleanupOldItemsAsync(historyLimit);
                    if (deletedCount > 0)
                    {
                        Log.Information("启动时清理历史记录，删除了 {DeletedCount} 条超出上限的记录", deletedCount);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "启动时清理历史记录失败");
                }
            });
        }

        private void InitializeMainWindowStateTracking()
        {
            var mainWindow = MainWindow;
            if (mainWindow == null)
            {
                return;
            }

            UpdateMainWindowState(mainWindow);

            mainWindow.IsVisibleChanged += (_, _) => UpdateMainWindowState(mainWindow);
            mainWindow.Activated += (_, _) => UpdateMainWindowState(mainWindow);
            mainWindow.Deactivated += (_, _) => UpdateMainWindowState(mainWindow);
        }

        private void UpdateMainWindowState(Window mainWindow)
        {
            _isMainWindowVisible = mainWindow.IsVisible;
            _isMainWindowActive = mainWindow.IsActive;
        }

        /// <summary>
        /// 判断快捷键是否为 Win+V
        /// </summary>
        private static bool IsWinVHotkey(string? hotkey)
        {
            if (string.IsNullOrEmpty(hotkey)) return false;
            var normalized = hotkey.ToLowerInvariant().Replace(" ", "").Replace("+", "");
            return normalized == "winv" || normalized == "windowsv";
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Container?.Resolve<IClipboardChangeSource>().Stop();
            Container?.Resolve<IHotkeyService>().ClearAllHotKeys();
            Container?.Resolve<IKeyboardHook>().Stop();
            Container?.Resolve<IForegroundWindowTracker>().Stop();

            // 停止主题监听
            Container?.Resolve<IThemeService>().StopSystemThemeMonitoring();

            // 释放托盘图标资源
            _trayIcon?.Dispose();
            _trayIcon = null;

            // Release mutex
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();

            base.OnExit(e);
            Log.Information("程序退出");
        }

        /// <summary>
        /// 切换主窗口显示/隐藏状态
        /// </summary>
        public void ToggleMainWindow()
        {
            if (MainWindow.Visibility == Visibility.Visible)
            {
                MainWindow.Close();
            }
            else
            {
                Container.Resolve<IMainWindowPositionService>().PositionMainWindow();
                NoActivateWindowController.ShowNoActivate(MainWindow);
            }
        }
    }
}
