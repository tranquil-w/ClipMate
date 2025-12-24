using ClipMate.UI.Bootstrap;
using Microsoft.Extensions.Configuration;
using Prism.Ioc;
using Serilog;
using Serilog.Core;

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
        SharedServiceRegistration.RegisterInfrastructure(
            new PrismServiceRegistryAdapter(containerRegistry),
            configuration,
            appDataFolder,
            loggingLevelSwitch,
            logger);
    }
}
