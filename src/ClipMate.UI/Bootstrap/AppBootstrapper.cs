using ClipMate.Infrastructure;
using Serilog.Core;
using Serilog.Events;

namespace ClipMate.UI.Bootstrap;

public static class AppBootstrapper
{
    public static AppBootstrapContext Initialize(string baseDirectory)
    {
        var configuration = AppConfigurationLoader.LoadConfiguration(baseDirectory);
        var appDataFolder = AppDataPathProvider.GetAppDataFolder();
        var loggingLevelSwitch = new LoggingLevelSwitch();

        var logLevel = AppSettingsReader.ReadSetting(
            settings => settings.LogLevel,
            LogEventLevel.Information,
            baseDirectory,
            appDataFolder,
            "读取日志级别配置时发生错误");
        loggingLevelSwitch.MinimumLevel = LogLevelPolicy.Normalize(logLevel);

        var logger = AppLoggingConfigurator.ConfigureLogging(appDataFolder, loggingLevelSwitch);

        return new AppBootstrapContext(configuration, appDataFolder, loggingLevelSwitch, logger);
    }
}
