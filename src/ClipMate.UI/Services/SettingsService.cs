using ClipMate.Core.Models;
using ClipMate.Infrastructure;
using ClipMate.Service.Interfaces;
using ClipMate.Messages;
using PlatformAutoStartService = ClipMate.Platform.Abstractions.Startup.IAutoStartService;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppConstants = ClipMate.Infrastructure.Constants;

namespace ClipMate.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly PlatformAutoStartService _autoStartService;
        private readonly LoggingLevelSwitch _loggingLevelSwitch;
        private readonly string _defaultSettingsFilePath;
        private readonly string _userSettingsDirectory;
        private readonly string _settingsFilePath;
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new LogEventLevelJsonConverter(), new JsonStringEnumConverter() }
        };
        private AppSettings _settings;

        public SettingsService(IConfiguration configuration, ILogger logger, PlatformAutoStartService autoStartService, LoggingLevelSwitch loggingLevelSwitch)
        {
            _configuration = configuration;
            _logger = logger;
            _autoStartService = autoStartService;
            _loggingLevelSwitch = loggingLevelSwitch;
            _defaultSettingsFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            _userSettingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppConstants.AppName);
            _settingsFilePath = Path.Combine(_userSettingsDirectory, "settings.json");
            _settings = CreateDefaultSettings();
            LoadSettings();
        }

        public string? GetHotKey()
        {
            return _settings.HotKey;
        }

        public void SetHotKey(string hotKey)
        {
            var oldValue = _settings.HotKey;
            _settings.HotKey = hotKey;

            if (!string.Equals(oldValue, hotKey, StringComparison.Ordinal))
            {
                WeakReferenceMessenger.Default.Send(new HotKeyChangedMessage(hotKey));
            }
        }

        public string? GetFavoriteFilterHotKey()
        {
            return _settings.FavoriteFilterHotKey;
        }

        public void SetFavoriteFilterHotKey(string hotKey)
        {
            var oldValue = _settings.FavoriteFilterHotKey;
            _settings.FavoriteFilterHotKey = hotKey;

            if (!string.Equals(oldValue, hotKey, StringComparison.Ordinal))
            {
                WeakReferenceMessenger.Default.Send(new FavoriteFilterHotKeyChangedMessage(hotKey));
            }
        }

        public string GetTheme()
        {
            return _settings.Theme ?? "System";
        }

        public void SetTheme(string theme)
        {
            _settings.Theme = theme;
        }

        public LogEventLevel GetLogLevel()
        {
            return _settings.LogLevel;
        }

        public void SetLogLevel(LogEventLevel level)
        {
            var normalizedLevel = LogLevelPolicy.Normalize(level);
            if (_settings.LogLevel == normalizedLevel)
            {
                return;
            }

            _settings.LogLevel = normalizedLevel;
            ApplyLogLevel();
        }

        public WindowPosition GetWindowPosition()
        {
            return _settings.WindowPosition;
        }

        public void SetWindowPosition(WindowPosition position)
        {
            _settings.WindowPosition = position;
        }

        public bool GetEnableWinComboGuardInjection()
        {
            return _settings.EnableWinComboGuardInjection;
        }

        public void SetEnableWinComboGuardInjection(bool enabled)
        {
            var oldValue = _settings.EnableWinComboGuardInjection;
            _settings.EnableWinComboGuardInjection = enabled;

            if (oldValue != enabled)
            {
                WeakReferenceMessenger.Default.Send(new WinComboGuardInjectionChangedMessage(enabled));
            }
        }

        public bool GetImeHintsEnabled()
        {
            return _settings.ImeHintsEnabled;
        }

        public void SetImeHintsEnabled(bool enabled)
        {
            var oldValue = _settings.ImeHintsEnabled;
            _settings.ImeHintsEnabled = enabled;

            if (oldValue != enabled)
            {
                WeakReferenceMessenger.Default.Send(new ImeHintsEnabledChangedMessage(enabled));
            }
        }

        public bool GetAutoStart()
        {
            return _settings.AutoStart;
        }

        public void SetAutoStart(bool enabled)
        {
            var oldValue = _settings.AutoStart;
            _settings.AutoStart = enabled;

            // 如果设置发生变化,立即同步到注册表
            if (oldValue != enabled)
            {
                _autoStartService.SetAutoStart(enabled);
            }
        }

        public bool GetSilentStart()
        {
            return _settings.SilentStart;
        }

        public void SetSilentStart(bool value)
        {
            _settings.SilentStart = value;
        }

        public bool GetAlwaysRunAsAdmin()
        {
            return _settings.AlwaysRunAsAdmin;
        }

        public void SetAlwaysRunAsAdmin(bool value)
        {
            _settings.AlwaysRunAsAdmin = value;
        }

        public int GetHistoryLimit()
        {
            return _settings.HistoryLimit;
        }

        public void SetHistoryLimit(int limit)
        {
            _settings.HistoryLimit = limit;
        }

        public int GetClipboardItemMaxHeight()
        {
            return _settings.ClipboardItemMaxHeight;
        }

        public void SetClipboardItemMaxHeight(int height)
        {
            var oldValue = _settings.ClipboardItemMaxHeight;
            if (oldValue != height)
            {
                _settings.ClipboardItemMaxHeight = height;
                WeakReferenceMessenger.Default.Send(new ClipboardItemMaxHeightChangedMessage(height));
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                Directory.CreateDirectory(_userSettingsDirectory);
                var json = JsonSerializer.Serialize(_settings, _serializerOptions);

                await File.WriteAllTextAsync(_settingsFilePath, json);
                _logger.Information("设置保存成功");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "保存设置时发生错误");
                throw;
            }
        }

        public async Task LoadAsync()
        {
            await Task.Run(() => LoadSettings());
        }

        private void LoadSettings()
        {
            try
            {
                var effectiveSettings = CreateDefaultSettings();
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json, _serializerOptions);

                    if (loadedSettings != null)
                    {
                        ApplyOverrides(effectiveSettings, loadedSettings);
                    }
                }

                EnsureEssentialDefaults(effectiveSettings, _configuration);
                _settings = effectiveSettings;
                ApplyLogLevel();

                _logger.Information("设置加载成功");

                // 异步检查并同步自启动状态
                Task.Run(async () =>
                {
                    await Task.Delay(1000); // 延迟一秒，确保应用完全启动
                    try
                    {
                        var registryEnabled = _autoStartService.IsAutoStartEnabled();
                        if (registryEnabled != _settings.AutoStart)
                        {
                            _logger.Information($"检测到自启动状态不一致，设置：{_settings.AutoStart}，注册表：{registryEnabled}，正在同步...");
                            // 使用注册表中的实际状态更新设置
                            _settings.AutoStart = registryEnabled;
                            // 保存更新后的设置
                            await SaveAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "同步自启动状态时发生错误");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "加载设置时发生错误，使用默认设置");
                _settings = CreateDefaultSettings();
                EnsureEssentialDefaults(_settings, _configuration);
                ApplyLogLevel();
            }
        }

        private AppSettings CreateDefaultSettings()
        {
            var defaults = new AppSettings();

            try
            {
                if (File.Exists(_defaultSettingsFilePath))
                {
                    var json = File.ReadAllText(_defaultSettingsFilePath);
                    var fileSettings = JsonSerializer.Deserialize<AppSettings>(json, _serializerOptions);
                    if (fileSettings != null)
                    {
                        ApplyOverrides(defaults, fileSettings);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "读取默认配置文件时发生错误，使用内置默认值");
            }

            var configuredHotKey = _configuration["HotKey"];
            if (!string.IsNullOrWhiteSpace(configuredHotKey))
            {
                defaults.HotKey = configuredHotKey;
            }

            var configuredFavoriteFilterHotKey = _configuration["FavoriteFilterHotKey"];
            if (!string.IsNullOrWhiteSpace(configuredFavoriteFilterHotKey))
            {
                defaults.FavoriteFilterHotKey = configuredFavoriteFilterHotKey;
            }

            var configuredTheme = _configuration["Theme"];
            if (!string.IsNullOrWhiteSpace(configuredTheme))
            {
                defaults.Theme = configuredTheme;
            }

            var enableWinComboGuardInjection = _configuration.GetValue<bool?>("EnableWinComboGuardInjection");
            if (enableWinComboGuardInjection.HasValue)
            {
                defaults.EnableWinComboGuardInjection = enableWinComboGuardInjection.Value;
            }

            var imeHintsEnabled = _configuration.GetValue<bool?>("ImeHintsEnabled");
            if (imeHintsEnabled.HasValue)
            {
                defaults.ImeHintsEnabled = imeHintsEnabled.Value;
            }

            var historyLimit = _configuration.GetValue<int?>("HistoryLimit");   
            if (historyLimit.HasValue && historyLimit.Value > 0)
            {
                defaults.HistoryLimit = historyLimit.Value;
            }

            var clipboardItemMaxHeight = _configuration.GetValue<int?>("ClipboardItemMaxHeight");
            if (clipboardItemMaxHeight.HasValue && clipboardItemMaxHeight.Value > 0)
            {
                defaults.ClipboardItemMaxHeight = clipboardItemMaxHeight.Value;
            }

            var configuredWindowPosition = _configuration["WindowPosition"];
            if (!string.IsNullOrWhiteSpace(configuredWindowPosition) &&
                Enum.TryParse<WindowPosition>(configuredWindowPosition, true, out var windowPosition) &&
                Enum.IsDefined(typeof(WindowPosition), windowPosition))
            {
                defaults.WindowPosition = windowPosition;
            }

            var autoStart = _configuration.GetValue<bool?>("AutoStart");        
            if (autoStart.HasValue)
            {
                defaults.AutoStart = autoStart.Value;
            }

            var silentStart = _configuration.GetValue<bool?>("SilentStart");
            if (silentStart.HasValue)
            {
                defaults.SilentStart = silentStart.Value;
            }

            var alwaysRunAsAdmin = _configuration.GetValue<bool?>("AlwaysRunAsAdmin");
            if (alwaysRunAsAdmin.HasValue)
            {
                defaults.AlwaysRunAsAdmin = alwaysRunAsAdmin.Value;
            }

            var configuredLogLevel = _configuration["LogLevel"];
            if (!string.IsNullOrWhiteSpace(configuredLogLevel) &&
                Enum.TryParse<LogEventLevel>(configuredLogLevel, true, out var logLevel))
            {
                defaults.LogLevel = LogLevelPolicy.Normalize(logLevel);
            }

            var connectionString = _configuration.GetConnectionString("ClipMateDb");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                defaults.ConnectionStrings ??= new ConnectionStrings();
                defaults.ConnectionStrings.ClipMateDb = connectionString;
            }

            return defaults;
        }

        private static void ApplyOverrides(AppSettings target, AppSettings overrides)
        {
            if (!string.IsNullOrWhiteSpace(overrides.HotKey))
            {
                target.HotKey = overrides.HotKey;
            }

            if (!string.IsNullOrWhiteSpace(overrides.FavoriteFilterHotKey))
            {
                target.FavoriteFilterHotKey = overrides.FavoriteFilterHotKey;
            }

            if (!string.IsNullOrWhiteSpace(overrides.Theme))
            {
                target.Theme = overrides.Theme;
            }

            target.AutoStart = overrides.AutoStart;
            target.SilentStart = overrides.SilentStart;
            target.AlwaysRunAsAdmin = overrides.AlwaysRunAsAdmin;

            if (overrides.HistoryLimit > 0)
            {
                target.HistoryLimit = overrides.HistoryLimit;
            }

            if (overrides.ClipboardItemMaxHeight > 0)
            {
                target.ClipboardItemMaxHeight = overrides.ClipboardItemMaxHeight;
            }

            if (Enum.IsDefined(typeof(WindowPosition), overrides.WindowPosition))
            {
                target.WindowPosition = overrides.WindowPosition;
            }

            target.EnableWinComboGuardInjection = overrides.EnableWinComboGuardInjection;
            target.ImeHintsEnabled = overrides.ImeHintsEnabled;

            if (overrides.ConnectionStrings?.ClipMateDb is { } clipMateDb &&    
                !string.IsNullOrWhiteSpace(clipMateDb))
            {
                target.ConnectionStrings ??= new ConnectionStrings();
                target.ConnectionStrings.ClipMateDb = clipMateDb;
            }

            target.LogLevel = LogLevelPolicy.Normalize(overrides.LogLevel);
        }

        private static void EnsureEssentialDefaults(AppSettings settings, IConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(settings.HotKey))
            {
                var configuredHotKey = configuration["HotKey"];
                if (!string.IsNullOrWhiteSpace(configuredHotKey))
                {
                    settings.HotKey = configuredHotKey;
                }
            }

            if (string.IsNullOrWhiteSpace(settings.FavoriteFilterHotKey))
            {
                var configuredFavoriteFilterHotKey = configuration["FavoriteFilterHotKey"];
                if (!string.IsNullOrWhiteSpace(configuredFavoriteFilterHotKey))
                {
                    settings.FavoriteFilterHotKey = configuredFavoriteFilterHotKey;
                }
            }

            if (string.IsNullOrWhiteSpace(settings.Theme))
            {
                var configuredTheme = configuration["Theme"];
                if (!string.IsNullOrWhiteSpace(configuredTheme))
                {
                    settings.Theme = configuredTheme;
                }
            }

            // 确保 SilentStart 有默认值（虽然 bool 默认为 false，但显式设置更清晰）
            // 由于 bool 类型无法判断是否被设置，这里不需要特殊处理

            if (settings.HistoryLimit <= 0)
            {
                var historyLimit = configuration.GetValue<int?>("HistoryLimit");
                if (historyLimit.HasValue && historyLimit.Value > 0)
                {
                    settings.HistoryLimit = historyLimit.Value;
                }
                else
                {
                    settings.HistoryLimit = 500;
                }
            }

            if (settings.ClipboardItemMaxHeight <= 0)
            {
                var clipboardItemMaxHeight = configuration.GetValue<int?>("ClipboardItemMaxHeight");
                if (clipboardItemMaxHeight.HasValue && clipboardItemMaxHeight.Value > 0)
                {
                    settings.ClipboardItemMaxHeight = clipboardItemMaxHeight.Value;
                }
                else
                {
                    settings.ClipboardItemMaxHeight = 100;
                }
            }

            if (!Enum.IsDefined(typeof(WindowPosition), settings.WindowPosition))
            {
                var configuredWindowPosition = configuration["WindowPosition"];
                if (!string.IsNullOrWhiteSpace(configuredWindowPosition) &&
                    Enum.TryParse<WindowPosition>(configuredWindowPosition, true, out var windowPosition) &&
                    Enum.IsDefined(typeof(WindowPosition), windowPosition))
                {
                    settings.WindowPosition = windowPosition;
                }
                else
                {
                    settings.WindowPosition = WindowPosition.FollowCaret;
                }
            }

            settings.ConnectionStrings ??= new ConnectionStrings();

            if (string.IsNullOrWhiteSpace(settings.ConnectionStrings.ClipMateDb))
            {
                var connectionString = configuration.GetConnectionString("ClipMateDb");
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    settings.ConnectionStrings.ClipMateDb = connectionString;
                }
            }

            if (!LogLevelPolicy.IsSupported(settings.LogLevel))
            {
                var configuredLogLevel = configuration["LogLevel"];
                if (!string.IsNullOrWhiteSpace(configuredLogLevel) &&
                    Enum.TryParse<LogEventLevel>(configuredLogLevel, true, out var logLevel))
                {
                    settings.LogLevel = LogLevelPolicy.Normalize(logLevel);
                }
                else
                {
                    settings.LogLevel = LogLevelPolicy.Normalize(settings.LogLevel);
                }
            }
            else
            {
                settings.LogLevel = LogLevelPolicy.Normalize(settings.LogLevel);
            }
        }

        private void ApplyLogLevel()
        {
            _loggingLevelSwitch.MinimumLevel = _settings.LogLevel;
        }

        public string GetUserFolder()
        {
            return _userSettingsDirectory;
        }
    }

    public class AppSettings
    {
        public string? HotKey { get; set; }
        public string? FavoriteFilterHotKey { get; set; }
        public string? Theme { get; set; }
        public bool EnableWinComboGuardInjection { get; set; }
        public bool ImeHintsEnabled { get; set; }
        public bool AutoStart { get; set; }
        public bool SilentStart { get; set; }
        public bool AlwaysRunAsAdmin { get; set; }
        public int HistoryLimit { get; set; }
        public int ClipboardItemMaxHeight { get; set; }
        public WindowPosition WindowPosition { get; set; }
        public LogEventLevel LogLevel { get; set; }
        public ConnectionStrings? ConnectionStrings { get; set; }
    }

    public class ConnectionStrings
    {
        public string? ClipMateDb { get; set; }
    }
}

