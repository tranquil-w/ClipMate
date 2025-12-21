using ClipMate.Service.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Prism.Ioc;
using Serilog;
using Serilog.Core;
using System.IO;

namespace ClipMate.Composition;

internal static class InfrastructureModule
{
    internal static void RegisterInfrastructure(
        this IContainerRegistry containerRegistry,
        IConfiguration configuration,
        string appDataFolder,
        LoggingLevelSwitch loggingLevelSwitch,
        ILogger logger)
    {
        containerRegistry.RegisterInstance(configuration);
        containerRegistry.RegisterInstance(loggingLevelSwitch);
        containerRegistry.RegisterInstance(logger);

        containerRegistry.RegisterInstance<IMessenger>(WeakReferenceMessenger.Default);

        var connectionString = configuration.GetConnectionString("ClipMateDb");
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Database connection string 'ClipMateDb' is not configured in appsettings.json");

        var dbPath = Path.Combine(appDataFolder, "ClipMate.db");
        var fullConnectionString = connectionString.Replace("ClipMate.db", dbPath);
        var connectionFactory = new SqliteConnectionFactory(fullConnectionString);
        containerRegistry.RegisterInstance<ISqliteConnectionFactory>(connectionFactory);
        containerRegistry.RegisterSingleton<IDatabaseService, DatabaseService>();
    }
}
