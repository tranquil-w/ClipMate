using ClipMate.Service.Clipboard;
using ClipMate.Service.Infrastructure;
using ClipMate.Service.Interfaces;
using ClipMate.Services;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;

namespace ClipMate.UI.Bootstrap;

public static class SharedServiceRegistration
{
    public static void RegisterInfrastructure(
        IAppServiceRegistry registry,
        IConfiguration configuration,
        string appDataFolder,
        LoggingLevelSwitch loggingLevelSwitch,
        ILogger logger)
    {
        registry.RegisterInstance(configuration);
        registry.RegisterInstance(loggingLevelSwitch);
        registry.RegisterInstance(logger);
        registry.RegisterInstance<IMessenger>(WeakReferenceMessenger.Default);

        var connectionString = configuration.GetConnectionString("ClipMateDb");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string 'ClipMateDb' is not configured in appsettings.json");
        }

        var dbPath = Path.Combine(appDataFolder, "ClipMate.db");
        var fullConnectionString = connectionString.Replace("ClipMate.db", dbPath);
        var connectionFactory = new SqliteConnectionFactory(fullConnectionString);
        registry.RegisterInstance<ISqliteConnectionFactory>(connectionFactory);
        registry.RegisterSingleton<IDatabaseService, DatabaseService>();
    }

    public static void RegisterServiceLayer(IAppServiceRegistry registry)
    {
        registry.RegisterSingleton<IClipboardItemRepository, DatabaseClipboardItemRepository>();
        registry.RegisterSingleton<IClipboardCaptureUseCase, ClipboardCaptureUseCase>();
        registry.RegisterSingleton<IPasteTargetWindowService, PasteTargetWindowService>();
        registry.RegisterSingleton<IClipboardPasteUseCase, ClipboardPasteUseCase>();
        registry.RegisterSingleton<IClipboardHistoryUseCase, ClipboardHistoryUseCase>();
    }

    public static void RegisterSharedAppServices(IAppServiceRegistry registry)
    {
        registry.RegisterSingleton<ISettingsService, SettingsService>();
        registry.RegisterSingleton<IHotkeyService, HotkeyServiceAdapter>();
        registry.RegisterSingleton<IAdminService, AdminService>();
    }
}
