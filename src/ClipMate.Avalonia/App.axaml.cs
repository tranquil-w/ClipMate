using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ClipMate.Avalonia.Infrastructure;
using ClipMate.Avalonia.Services;
using ClipMate.Avalonia.ViewModels;
using ClipMate.Messages;
using ClipMate.Platform.Abstractions.Clipboard;
using ClipMate.Platform.Abstractions.Input;
using ClipMate.Platform.Abstractions.Startup;
using ClipMate.Platform.Abstractions.Tray;
using ClipMate.Platform.Abstractions.Window;
using ClipMate.Platform.Windows.Clipboard;
using ClipMate.Platform.Windows.Input;
using ClipMate.Platform.Windows.Startup;
using ClipMate.Platform.Windows.Windowing;
using ClipMate.Service.Clipboard;
using ClipMate.Service.Infrastructure;
using ClipMate.Service.Interfaces;
using ClipMate.Service.Windowing;
using ClipMate.Services;
using ClipMate.UI.Abstractions;
using ClipMate.UI.Bootstrap;
using ClipMate.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SharpHook;

namespace ClipMate.Avalonia;

public partial class App : Application
{
    private const string MutexName = "ClipMate";
    private static Mutex? _mutex;
    private ITrayIcon? _trayIcon;
    private volatile bool _isMainWindowVisible;
    private volatile bool _isMainWindowActive;
    private string _appDataFolder = string.Empty;
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        if (TryRestartAsAdminIfNeeded())
        {
            RequestShutdown(desktop);
            return;
        }

        _mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            RequestShutdown(desktop);
            return;
        }

        var bootstrap = AppBootstrapper.Initialize(AppContext.BaseDirectory);
        _appDataFolder = bootstrap.AppDataFolder;

        var services = new ServiceCollection();
        ConfigureServices(services, bootstrap);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        desktop.MainWindow = mainWindow;
        desktop.Exit += (_, _) => HandleExit();

        var themeService = _serviceProvider.GetRequiredService<IThemeService>();
        themeService.ApplyTheme(themeService.GetCurrentTheme());
        themeService.StartSystemThemeMonitoring();

        var silentStart = ReadSilentStartSetting();
        if (!silentStart)
        {
            NoActivateWindowController.ShowNoActivate(mainWindow);
        }

        InitializeMainWindowStateTracking(mainWindow);
        InitializeNotifyIcon();
        InitializeBackgroundServices();
        _serviceProvider.GetRequiredService<MainWindowOverlayService>().Initialize(mainWindow);

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(ServiceCollection services, AppBootstrapContext bootstrap)
    {
        var registry = new ServiceCollectionRegistryAdapter(services);

        SharedServiceRegistration.RegisterInfrastructure(
            registry,
            bootstrap.Configuration,
            bootstrap.AppDataFolder,
            bootstrap.LoggingLevelSwitch,
            bootstrap.Logger);

        SharedServiceRegistration.RegisterServiceLayer(registry);
        SharedServiceRegistration.RegisterSharedAppServices(registry);

        services.AddSingleton<IUiDispatcher, AvaloniaUiDispatcher>();
        services.AddSingleton<IUserDialogService, AvaloniaUserDialogService>();
        services.AddSingleton<IThemeService, AvaloniaThemeService>();
        services.AddSingleton<IClipboardService, AvaloniaClipboardService>();
        services.AddSingleton<IApplicationService, AvaloniaApplicationService>();
        services.AddSingleton<IMainWindowPositionService, MainWindowPositionService>();
        services.AddSingleton<IMainWindowController>(sp =>
            new AvaloniaMainWindowController(
                () => sp.GetRequiredService<MainWindow>(),
                sp.GetRequiredService<IUiDispatcher>()));
        services.AddSingleton<ITrayIcon, AvaloniaTrayIcon>();
        services.AddSingleton<NotifyIconCommandHandler>();
        services.AddSingleton<MainWindowOverlayService>();

        services.AddSingleton<MainWindow>();
        services.AddTransient<Views.SettingsWindow>();
        services.AddSingleton<ClipboardViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<SettingsViewModel>();

        services.AddSingleton<IEventSimulator, EventSimulator>();
        services.AddSingleton<Func<IGlobalHook>>(() => new SimpleGlobalHook(SharpHook.Data.GlobalHookType.Keyboard, null, true));
        services.AddSingleton<IClipboardChangeSource, WindowsClipboardChangeSource>();
        services.AddSingleton<IClipboardWriter, WindowsClipboardWriter>();
        services.AddSingleton<IPasteTrigger, WindowsPasteTrigger>();
        services.AddSingleton<IWindowPositionProvider, WindowsWindowPositionProvider>();
        services.AddSingleton<IForegroundWindowService, WindowsForegroundWindowService>();
        services.AddSingleton<IForegroundWindowTracker, WindowsForegroundWindowTracker>();
        services.AddSingleton<IGlobalHotkeyService, WindowsGlobalHotkeyService>();
        services.AddSingleton<IKeyboardHook, WindowsKeyboardHook>();
        services.AddSingleton<IAutoStartService, WindowsAutoStartService>();
    }

    private bool TryRestartAsAdminIfNeeded()
    {
        try
        {
            var adminService = new ClipMate.Services.AdminService();
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

    private bool ReadAlwaysRunAsAdminSetting()
    {
        return AppSettingsReader.ReadSetting(
            s => s.AlwaysRunAsAdmin,
            false,
            AppContext.BaseDirectory,
            _appDataFolder,
            "读取管理员运行配置时发生错误",
            Log.Logger);
    }

    private bool ReadSilentStartSetting()
    {
        return AppSettingsReader.ReadSetting(
            s => s.SilentStart,
            false,
            AppContext.BaseDirectory,
            _appDataFolder,
            "读取静默启动配置时发生错误",
            Log.Logger);
    }

    private void InitializeNotifyIcon()
    {
        if (_serviceProvider == null)
        {
            return;
        }

        _trayIcon = _serviceProvider.GetRequiredService<ITrayIcon>();
        _trayIcon.ToolTip = "ClipMate";
        _trayIcon.SetIcon(new TrayIconSource.ResourceUri("avares://ClipMate.Avalonia/Assets/ClipMate.ico"));
        _trayIcon.SetContextMenu(
        [
            new TrayMenuItem
            {
                Title = "用户文件夹",
                OnClick = () => _serviceProvider.GetRequiredService<NotifyIconCommandHandler>().OpenUserFolder()
            },
            new TrayMenuItem
            {
                Title = "设置",
                OnClick = () => _serviceProvider.GetRequiredService<NotifyIconCommandHandler>().OpenSettings()
            },
            TrayMenuItem.Separator,
            new TrayMenuItem
            {
                Title = "退出",
                OnClick = () => _serviceProvider.GetRequiredService<IApplicationService>().Shutdown()
            }
        ]);

        _trayIcon.Show();
        _trayIcon.Clicked += (_, _) => Dispatcher.UIThread.InvokeAsync(ToggleMainWindow);
    }

    private void InitializeBackgroundServices()
    {
        if (_serviceProvider == null)
        {
            return;
        }

        Log.Information("程序启动");

        _ = Task.Run(async () =>
        {
            try
            {
                await _serviceProvider.GetRequiredService<IDatabaseService>().InitializeAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "数据库初始化失败");
            }
        });

        _serviceProvider.GetRequiredService<IClipboardChangeSource>().Start();
        _serviceProvider.GetRequiredService<IForegroundWindowTracker>().Start();

        var keyboardHook = _serviceProvider.GetRequiredService<IKeyboardHook>();
        keyboardHook.Start();

        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        var hotkeyService = _serviceProvider.GetRequiredService<IHotkeyService>();
        var currentHotkey = settingsService.GetHotKey();

        if (keyboardHook is WindowsKeyboardHook windowsKeyboardHook)
        {
            windowsKeyboardHook.EnableWinComboGuardInjection = settingsService.GetEnableWinComboGuardInjection();
            WeakReferenceMessenger.Default.Register<WinComboGuardInjectionChangedMessage>(
                this,
                (_, message) => windowsKeyboardHook.EnableWinComboGuardInjection = message.Value);
        }

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
            Dispatcher.UIThread.InvokeAsync(ToggleMainWindow);
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
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                WeakReferenceMessenger.Default.Send(new FavoriteFilterHotKeyPressedMessage());
            });
        };

        if (!IsWinVHotkey(currentHotkey))
        {
            hotkeyService.RegisterMainWindowToggleHotkey(ToggleMainWindow);
        }

        _ = Task.Run(async () =>
        {
            try
            {
                var historyLimit = settingsService.GetHistoryLimit();
                var historyUseCase = _serviceProvider.GetRequiredService<IClipboardHistoryUseCase>();
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

    private void InitializeMainWindowStateTracking(Window mainWindow)
    {
        UpdateMainWindowState(mainWindow);
        mainWindow.PropertyChanged += (_, e) =>
        {
            if (e.Property == Visual.IsVisibleProperty)
            {
                UpdateMainWindowState(mainWindow);
            }
        };
        mainWindow.Activated += (_, _) => UpdateMainWindowState(mainWindow);
        mainWindow.Deactivated += (_, _) => UpdateMainWindowState(mainWindow);
    }

    private static void RequestShutdown(IClassicDesktopStyleApplicationLifetime desktop)
    {
        Dispatcher.UIThread.Post(() => desktop.Shutdown());
    }

    private void UpdateMainWindowState(Window mainWindow)
    {
        _isMainWindowVisible = mainWindow.IsVisible;
        _isMainWindowActive = mainWindow.IsActive;
    }

    private static bool IsWinVHotkey(string? hotkey)
    {
        if (string.IsNullOrEmpty(hotkey)) return false;
        var normalized = hotkey.ToLowerInvariant().Replace(" ", "").Replace("+", "");
        return normalized == "winv" || normalized == "windowsv";
    }

    public void ToggleMainWindow()
    {
        if (_serviceProvider == null)
        {
            return;
        }

        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow == null)
        {
            return;
        }

        var mainWindow = desktop.MainWindow;
        if (mainWindow.IsVisible)
        {
            mainWindow.Close();
            return;
        }

        _serviceProvider.GetRequiredService<IMainWindowPositionService>().PositionMainWindow();
        NoActivateWindowController.ShowNoActivate(mainWindow);
    }

    private void HandleExit()
    {
        if (_serviceProvider != null)
        {
            _serviceProvider.GetService<IClipboardChangeSource>()?.Stop();
            _serviceProvider.GetService<IHotkeyService>()?.ClearAllHotKeys();
            _serviceProvider.GetService<IKeyboardHook>()?.Stop();
            _serviceProvider.GetService<IForegroundWindowTracker>()?.Stop();
            _serviceProvider.GetService<IThemeService>()?.StopSystemThemeMonitoring();
        }

        _trayIcon?.Dispose();
        _trayIcon = null;

        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        Log.Information("程序退出");
    }
}
