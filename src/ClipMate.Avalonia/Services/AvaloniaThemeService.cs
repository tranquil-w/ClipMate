using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Styling;
using ClipMate.Service.Interfaces;
using Serilog;

namespace ClipMate.Avalonia.Services;

public sealed class AvaloniaThemeService : IThemeService
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger _logger;
    private bool _isMonitoring;

    public AvaloniaThemeService(ISettingsService settingsService, ILogger logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public string GetCurrentTheme()
    {
        return _settingsService.GetTheme() ?? "System";
    }

    public void ApplyTheme(string theme)
    {
        _settingsService.SetTheme(theme);
        _ = _settingsService.SaveAsync();
        ApplyThemeVariant(theme);
    }

    public void StartSystemThemeMonitoring()
    {
        if (_isMonitoring)
        {
            return;
        }

        var platformSettings = Application.Current?.PlatformSettings;
        if (platformSettings == null)
        {
            return;
        }

        platformSettings.ColorValuesChanged += OnColorValuesChanged;
        _isMonitoring = true;

        if (string.Equals(GetCurrentTheme(), "System", StringComparison.OrdinalIgnoreCase))
        {
            ApplyThemeVariant("System");
        }
    }

    public void StopSystemThemeMonitoring()
    {
        if (!_isMonitoring)
        {
            return;
        }

        var platformSettings = Application.Current?.PlatformSettings;
        if (platformSettings != null)
        {
            platformSettings.ColorValuesChanged -= OnColorValuesChanged;
        }

        _isMonitoring = false;
    }

    private void OnColorValuesChanged(object? sender, PlatformColorValues e)
    {
        if (!string.Equals(GetCurrentTheme(), "System", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ApplyThemeVariant("System");
    }

    private void ApplyThemeVariant(string theme)
    {
        if (Application.Current == null)
        {
            _logger.Warning("Avalonia 应用未初始化，无法应用主题");
            return;
        }

        Application.Current.RequestedThemeVariant = theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }
}
