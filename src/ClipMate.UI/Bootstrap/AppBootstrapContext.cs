using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;

namespace ClipMate.UI.Bootstrap;

public sealed class AppBootstrapContext
{
    public AppBootstrapContext(
        IConfiguration configuration,
        string appDataFolder,
        LoggingLevelSwitch loggingLevelSwitch,
        ILogger logger)
    {
        Configuration = configuration;
        AppDataFolder = appDataFolder;
        LoggingLevelSwitch = loggingLevelSwitch;
        Logger = logger;
    }

    public IConfiguration Configuration { get; }

    public string AppDataFolder { get; }

    public LoggingLevelSwitch LoggingLevelSwitch { get; }

    public ILogger Logger { get; }
}
