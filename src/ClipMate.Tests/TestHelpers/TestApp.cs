using ClipMate.Service.Interfaces;
using ClipMate.UI.Abstractions;
using ClipMate.Platform.Abstractions.Clipboard;
using ClipMate.Services;
using ClipMate.Service.Clipboard;
using ClipMate.Service.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using SharpHook;
using System.Windows;
using Serilog;

namespace ClipMate.Tests.TestHelpers;

public class TestApp : PrismApplication
{
    private readonly string[] _resourceDictionaries =
    [
        "pack://application:,,,/HandyControl;component/Themes/SkinDark.xaml",
        "pack://application:,,,/HandyControl;component/Themes/Theme.xaml",
        "pack://application:,,,/ClipMate;component/Themes/Converters.xaml",
        "pack://application:,,,/ClipMate;component/Themes/DataTemplate.xaml",
        "pack://application:,,,/ClipMate;component/Themes/Settings.xaml",
        "pack://application:,,,/ClipMate;component/Themes/Styles.xaml",
    ];

    public IConfiguration? Configuration { get; private set; }

    protected override Window CreateShell()
    {
        foreach (var uri in _resourceDictionaries)
        {
            Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(uri) });
        }

        return Container.Resolve<Window>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 构建 IConfiguration 对象，加载 appsettings.json
        Configuration = TestServiceProviderFactory.CreateConfiguration();
        containerRegistry.RegisterInstance(Configuration);

        // 注册测试用的 Logger（使用静默 logger，不输出日志）
        var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .CreateLogger();
        containerRegistry.RegisterInstance<ILogger>(logger);

        // 注册键盘鼠标事件模拟器
        containerRegistry.RegisterSingleton<IEventSimulator, EventSimulator>();

        // 注册 IMessenger 实现
        containerRegistry.RegisterInstance<IMessenger>(WeakReferenceMessenger.Default);

        // 删除数据库文件
        var connectionString = Configuration.GetConnectionString("ClipMateDb");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var builder = new SqliteConnectionStringBuilder(connectionString);
            if (!string.IsNullOrWhiteSpace(builder.DataSource) && File.Exists(builder.DataSource))
            {
                File.Delete(builder.DataSource);
            }
        }

        // 注册数据库连接工厂
        containerRegistry.RegisterInstance<ISqliteConnectionFactory>(new SqliteConnectionFactory(connectionString!));

        // Register services
        containerRegistry.RegisterSingleton<IThemeService, ThemeService>();
        containerRegistry.RegisterSingleton<IDatabaseService, DatabaseService>();
        containerRegistry.RegisterSingleton<IWindowSwitchService, WindowSwitchService>();
        containerRegistry.RegisterSingleton<IClipboardChangeSource, FakeClipboardChangeSource>();
        containerRegistry.RegisterSingleton<IClipboardService, ClipboardService>();
        containerRegistry.RegisterSingleton<IHotkeyService, FakeHotkeyService>();
        containerRegistry.RegisterSingleton<IClipboardItemRepository, DatabaseClipboardItemRepository>();
        containerRegistry.RegisterSingleton<IClipboardCaptureUseCase, ClipboardCaptureUseCase>();
        containerRegistry.RegisterSingleton<IClipboardHistoryUseCase, ClipboardHistoryUseCase>();
        containerRegistry.RegisterSingleton<IClipboardPasteUseCase, ClipboardPasteUseCase>();
        containerRegistry.RegisterSingleton<IUiDispatcher, ImmediateUiDispatcher>();
        containerRegistry.RegisterSingleton<IUserDialogService, FakeUserDialogService>();

        //// Register views with regions
        //var regionManager = containerRegistry.GetContainer().Resolve<IRegionManager>();
        //regionManager.RegisterViewWithRegion(RegionNames.MainRegion, typeof(ClipboardView));
        //regionManager.RegisterViewWithRegion(RegionNames.TrayRegion, typeof(NotifyIconView));
    }

    protected override void ConfigureViewModelLocator()
    {
        base.ConfigureViewModelLocator();

        //ViewModelLocationProvider.Register<ClipboardView, ClipboardViewModel>();
        //ViewModelLocationProvider.Register<NotifyIconView, NotifyIconViewModel>();
    }
}
