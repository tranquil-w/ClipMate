using ClipMate.Services;
using ClipMate.Infrastructure;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClipMate.UI.Bootstrap;

public static class AppSettingsReader
{
    public static JsonSerializerOptions CreateSettingsJsonOptions()
    {
        return new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new LogEventLevelJsonConverter(), new JsonStringEnumConverter() }
        };
    }

    public static T ReadSetting<T>(
        Func<AppSettings, T?> propertySelector,
        T defaultValue,
        string baseDirectory,
        string appDataFolder,
        string? errorMessage = null,
        ILogger? logger = null) where T : struct
    {
        try
        {
            var options = CreateSettingsJsonOptions();
            var defaultSettingsPath = Path.Combine(baseDirectory, "appsettings.json");
            var userSettingsPath = Path.Combine(appDataFolder, "settings.json");

            T? value = null;

            if (File.Exists(defaultSettingsPath))
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(defaultSettingsPath), options);
                if (settings != null)
                {
                    value = propertySelector(settings);
                }
            }

            if (File.Exists(userSettingsPath))
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(userSettingsPath), options);
                if (settings != null)
                {
                    value = propertySelector(settings);
                }
            }

            return value ?? defaultValue;
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrWhiteSpace(errorMessage) && logger != null)
            {
                logger.Error(ex, errorMessage);
            }
            return defaultValue;
        }
    }
}
