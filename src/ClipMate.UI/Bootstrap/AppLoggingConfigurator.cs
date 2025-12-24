using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace ClipMate.UI.Bootstrap;

public static class AppLoggingConfigurator
{
    public static ILogger ConfigureLogging(string appDataFolder, LoggingLevelSwitch loggingLevelSwitch)
    {
        var logFolder = Path.Combine(appDataFolder, "Logs");
        Directory.CreateDirectory(logFolder);

        var logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(loggingLevelSwitch)
            .WriteTo.Debug()
            .WriteTo.File(Path.Combine(logFolder, "clipmate-.log"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Logger = logger;
        return logger;
    }
}
