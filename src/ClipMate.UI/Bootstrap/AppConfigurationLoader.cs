using Microsoft.Extensions.Configuration;

namespace ClipMate.UI.Bootstrap;

public static class AppConfigurationLoader
{
    public static IConfiguration LoadConfiguration(string baseDirectory)
    {
        return new ConfigurationBuilder()
            .SetBasePath(baseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }
}
