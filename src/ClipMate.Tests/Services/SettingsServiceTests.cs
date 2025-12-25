using System.Reflection;
using System.Text.Json;
using ClipMate.Core.Models;
using ClipMate.Service.Interfaces;
using ClipMate.Messages;
using ClipMate.Services;
using ClipMate.Tests.TestHelpers;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Moq;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using PlatformAutoStartService = ClipMate.Platform.Abstractions.Startup.IAutoStartService;

namespace ClipMate.Tests.Services
{
    /// <summary>
    /// SettingsService 的关键行为单元测试，覆盖配置加载/保存、属性读写、路径处理与默认值回退逻辑。
    /// </summary>
    public class SettingsServiceTests : TestBase
    {
        private readonly Mock<ILogger> _loggerMock = new();
        private readonly Mock<PlatformAutoStartService> _autoStartServiceMock = new();

        public SettingsServiceTests()
        {
            // 避免构造函数异步校验写入真实路径，保持返回默认 false 以绕过同步逻辑
            _autoStartServiceMock.Setup(s => s.IsAutoStartEnabled()).Returns(false);
            _autoStartServiceMock.Setup(s => s.SetAutoStart(It.IsAny<bool>()));
        }

        public override void Dispose()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
            base.Dispose();
        }

        /// <summary>
        /// SaveAsync 应将当前内存中的设置写入用户目录下的 settings.json，且自动创建缺失的目录。
        /// </summary>
        [Fact]
        public async Task SaveAsync_ShouldPersistSettingsIntoTempUserFolder()
        {
            var config = BuildConfiguration();
            var tempRoot = CreateTempRoot();
            var service = CreateServiceWithTempPaths(config, tempRoot, out var userDir, out var settingsPath, out var levelSwitch);

            service.SetHotKey("Ctrl+Alt+9");
            service.SetFavoriteFilterHotKey("Ctrl+Alt+F");
            service.SetTheme("Dark");
            service.SetAutoStart(true);
            service.SetHistoryLimit(123);
            service.SetClipboardItemMinHeight(120);
            service.SetClipboardItemMaxHeight(456);
            service.SetLogLevel(LogEventLevel.Debug);

            await service.SaveAsync();

            Assert.True(File.Exists(settingsPath));
            Assert.True(Directory.Exists(userDir));

            using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(settingsPath));
            var root = doc.RootElement;
            Assert.Equal("Ctrl+Alt+9", root.GetProperty("HotKey").GetString());
            Assert.Equal("Ctrl+Alt+F", root.GetProperty("FavoriteFilterHotKey").GetString());
            Assert.Equal("Dark", root.GetProperty("Theme").GetString());
            Assert.True(root.GetProperty("AutoStart").GetBoolean());
            Assert.Equal(123, root.GetProperty("HistoryLimit").GetInt32());
            Assert.Equal(120, root.GetProperty("ClipboardItemMinHeight").GetInt32());
            Assert.Equal(456, root.GetProperty("ClipboardItemMaxHeight").GetInt32());
            Assert.Equal(nameof(LogEventLevel.Debug), root.GetProperty("LogLevel").GetString());
            Assert.Equal(userDir, service.GetUserFolder());
            Assert.Equal(LogEventLevel.Debug, levelSwitch.MinimumLevel);

            _autoStartServiceMock.Verify(s => s.SetAutoStart(true), Times.Once);
        }

        /// <summary>
        /// LoadAsync 应优先加载默认文件，再应用用户文件和 IConfiguration 覆盖，最终补齐必要的默认值。
        /// </summary>
        [Fact]
        public async Task LoadAsync_ShouldMergeDefaultUserFileAndConfiguration()
        {
            var configurationValues = new Dictionary<string, string?>
            {
                ["Theme"] = "ConfigTheme",
                ["HistoryLimit"] = "250",
                ["AutoStart"] = "false",
                ["ConnectionStrings:ClipMateDb"] = "Data Source=config.db"
            };
            var config = BuildConfiguration(configurationValues);

            var tempRoot = CreateTempRoot();
            var defaultSettingsPath = Path.Combine(tempRoot, "default.appsettings.json");
            var defaultJson = """
            {
              "HotKey": "DefaultHotKey",
              "Theme": "Light",
              "HistoryLimit": 50,
              "ClipboardItemMinHeight": 70,
              "ClipboardItemMaxHeight": 80,
              "AutoStart": false,
              "LogLevel": "Warning",
              "ConnectionStrings": {
                "ClipMateDb": "Data Source=default.db"
              }
            }
            """;
            File.WriteAllText(defaultSettingsPath, defaultJson);

            var userDir = Path.Combine(tempRoot, "UserSettings");
            Directory.CreateDirectory(userDir);
            var userSettingsPath = Path.Combine(userDir, "settings.json");
            var userJson = """
            {
              "HotKey": "UserHotKey",
              "Theme": "",
              "HistoryLimit": 500,
              "AutoStart": true,
              "LogLevel": "Error",
              "ConnectionStrings": {
                "ClipMateDb": "Data Source=user.db"
              }
            }
            """;
            File.WriteAllText(userSettingsPath, userJson);

            var service = CreateServiceWithTempPaths(config, tempRoot, out _, out _, out var levelSwitch, defaultSettingsPath, userSettingsPath);

            await service.LoadAsync();

            Assert.Equal("UserHotKey", service.GetHotKey());          // 用户文件优先
            Assert.Equal("ConfigTheme", service.GetTheme());          // 用户未提供时走 IConfiguration
            Assert.True(service.GetAutoStart());                      // 用户文件覆盖布尔值
            Assert.Equal(500, service.GetHistoryLimit());             // 数值覆盖
            Assert.Equal(70, service.GetClipboardItemMinHeight());    // 默认文件中的有效值生效
            Assert.Equal(80, service.GetClipboardItemMaxHeight());    // 默认文件中的有效值生效
            Assert.Equal(LogEventLevel.Error, service.GetLogLevel()); // 用户文件覆盖日志级别
            Assert.Equal(userDir, service.GetUserFolder());
            Assert.Equal(LogEventLevel.Error, levelSwitch.MinimumLevel);

            var settings = GetPrivateSettings(service);
            Assert.Equal("Data Source=user.db", settings.ConnectionStrings?.ClipMateDb);
        }

        [Fact]
        public async Task LoadAsync_ShouldNormalizeInvalidLogLevelToInformation()
        {
            var config = BuildConfiguration();
            var tempRoot = CreateTempRoot();
            var userDir = Path.Combine(tempRoot, "UserSettings");
            Directory.CreateDirectory(userDir);
            var userSettingsPath = Path.Combine(userDir, "settings.json");

            var userJson = """
            {
              "HotKey": "UserHotKey",
              "LogLevel": "Verbose",
              "HistoryLimit": 25
            }
            """;
            File.WriteAllText(userSettingsPath, userJson);

            var service = CreateServiceWithTempPaths(config, tempRoot, out _, out _, out var levelSwitch, null, userSettingsPath);

            await service.LoadAsync();

            Assert.Equal(LogEventLevel.Information, service.GetLogLevel());
            Assert.Equal(LogEventLevel.Information, levelSwitch.MinimumLevel);
            Assert.Equal(25, service.GetHistoryLimit());
        }

        /// <summary>
        /// SetAutoStart 只有在值发生变化时才应调用依赖的 IAutoStartService。
        /// </summary>
        [Fact]
        public void SetAutoStart_ShouldInvokeDependencyOnlyWhenChanged()
        {
            var service = CreateServiceWithTempPaths(BuildConfiguration(), CreateTempRoot(), out _, out _, out _);

            service.SetAutoStart(true);  // 触发一次
            service.SetAutoStart(true);  // 不重复触发
            service.SetAutoStart(false); // 再触发一次

            _autoStartServiceMock.Verify(s => s.SetAutoStart(true), Times.Once);
            _autoStartServiceMock.Verify(s => s.SetAutoStart(false), Times.Once);
        }

        /// <summary>
        /// 修改剪贴项最大高度时应通过 WeakReferenceMessenger 广播变更消息。
        /// </summary>
        [Fact]
        public void SetClipboardItemMaxHeight_ShouldSendMessageOnChange()
        {
            WeakReferenceMessenger.Default.Reset();
            var service = CreateServiceWithTempPaths(BuildConfiguration(), CreateTempRoot(), out _, out _, out _);
            var received = new List<int>();

            WeakReferenceMessenger.Default.Register<ClipboardItemMaxHeightChangedMessage>(
                this,
                (_, msg) => received.Add(msg.Value));

            service.SetClipboardItemMaxHeight(200);
            service.SetClipboardItemMaxHeight(200); // 重复值不应再次发送

            Assert.Single(received);
            Assert.Contains(200, received);
        }

        /// <summary>
        /// 修改剪贴项最小高度时应通过 WeakReferenceMessenger 广播变更消息。
        /// </summary>
        [Fact]
        public void SetClipboardItemMinHeight_ShouldSendMessageOnChange()
        {
            WeakReferenceMessenger.Default.Reset();
            var service = CreateServiceWithTempPaths(BuildConfiguration(), CreateTempRoot(), out _, out _, out _);
            var received = new List<int>();

            WeakReferenceMessenger.Default.Register<ClipboardItemMinHeightChangedMessage>(
                this,
                (_, msg) => received.Add(msg.Value));

            service.SetClipboardItemMinHeight(120);
            service.SetClipboardItemMinHeight(120); // 重复值不应再次发送

            Assert.Single(received);
            Assert.Contains(120, received);
        }

        /// <summary>
        /// 当最小高度高于最大高度时，应同步提升最大高度。
        /// </summary>
        [Fact]
        public void SetClipboardItemMinHeight_ShouldAdjustMaxWhenMinExceedsMax()
        {
            var service = CreateServiceWithTempPaths(BuildConfiguration(), CreateTempRoot(), out _, out _, out _);

            service.SetClipboardItemMaxHeight(80);
            service.SetClipboardItemMinHeight(120);

            Assert.Equal(120, service.GetClipboardItemMinHeight());
            Assert.Equal(120, service.GetClipboardItemMaxHeight());
        }

        /// <summary>
        /// 当最大高度低于最小高度时，应同步降低最小高度。
        /// </summary>
        [Fact]
        public void SetClipboardItemMaxHeight_ShouldAdjustMinWhenMaxBelowMin()
        {
            var service = CreateServiceWithTempPaths(BuildConfiguration(), CreateTempRoot(), out _, out _, out _);

            service.SetClipboardItemMinHeight(120);
            service.SetClipboardItemMaxHeight(80);

            Assert.Equal(80, service.GetClipboardItemMinHeight());
            Assert.Equal(80, service.GetClipboardItemMaxHeight());
        }

        /// <summary>
        /// 默认值填充：当 HotKey/Theme/HistoryLimit 等缺失或非正数时，应回落到内置默认值。
        /// </summary>
        [Fact]
        public void EnsureEssentialDefaults_ShouldFillMissingValues()
        {
            var configurationValues = new Dictionary<string, string?>
            {
                ["HotKey"] = "Ctrl + `",
                ["FavoriteFilterHotKey"] = "Win + B",
                ["Theme"] = "System",
                ["HistoryLimit"] = "500",
                ["ClipboardItemMinHeight"] = "56",
                ["ClipboardItemMaxHeight"] = "56",
                ["WindowPosition"] = "FollowCaret",
                ["LogLevel"] = "Information",
                ["ConnectionStrings:ClipMateDb"] = "Data Source=ClipMate.db"
            };
            var config = BuildConfiguration(configurationValues);
            var method = typeof(SettingsService).GetMethod("EnsureEssentialDefaults", BindingFlags.NonPublic | BindingFlags.Static)
                         ?? throw new InvalidOperationException("未找到 EnsureEssentialDefaults 方法。");

            var settings = new AppSettings
            {
                HotKey = null,
                FavoriteFilterHotKey = null,
                Theme = "",
                HistoryLimit = 0,
                ClipboardItemMinHeight = 0,
                ClipboardItemMaxHeight = 0,
                ConnectionStrings = new ConnectionStrings { ClipMateDb = "" },  
                LogLevel = (LogEventLevel)999
            };

            method.Invoke(null, new object[] { settings, config });

            Assert.Equal("Ctrl + `", settings.HotKey);
            Assert.Equal("Win + B", settings.FavoriteFilterHotKey);
            Assert.Equal("System", settings.Theme);
            Assert.Equal(500, settings.HistoryLimit);
            Assert.Equal(56, settings.ClipboardItemMinHeight);
            Assert.Equal(56, settings.ClipboardItemMaxHeight);
            Assert.Equal("Data Source=ClipMate.db", settings.ConnectionStrings?.ClipMateDb);
            Assert.Equal(LogEventLevel.Information, settings.LogLevel);
        }

        /// <summary>
        /// 基础属性的读写应直接更新内存中的设置模型。
        /// </summary>
        [Fact]
        public void PropertySetters_ShouldUpdateInMemoryValues()
        {
            var service = CreateServiceWithTempPaths(BuildConfiguration(), CreateTempRoot(), out _, out _, out _);

            service.SetHotKey("Alt+F10");
            service.SetFavoriteFilterHotKey("Ctrl+Alt+H");
            service.SetTheme("Light");
            service.SetHistoryLimit(42);
            service.SetClipboardItemMinHeight(120);
            service.SetClipboardItemMaxHeight(300);
            service.SetLogLevel(LogEventLevel.Warning);

            Assert.Equal("Alt+F10", service.GetHotKey());
            Assert.Equal("Ctrl+Alt+H", service.GetFavoriteFilterHotKey());
            Assert.Equal("Light", service.GetTheme());
            Assert.Equal(42, service.GetHistoryLimit());
            Assert.Equal(120, service.GetClipboardItemMinHeight());
            Assert.Equal(300, service.GetClipboardItemMaxHeight());
            Assert.Equal(LogEventLevel.Warning, service.GetLogLevel());
        }

        /// <summary>
        /// SilentStart 属性的读写应直接更新内存中的设置。
        /// </summary>
        [Fact]
        public void SilentStart_ShouldUpdateInMemoryValue()
        {
            var service = CreateServiceWithTempPaths(BuildConfiguration(), CreateTempRoot(), out _, out _, out _);

            // 测试设置为 true
            service.SetSilentStart(true);
            Assert.True(service.GetSilentStart());

            // 测试设置为 false
            service.SetSilentStart(false);
            Assert.False(service.GetSilentStart());

            // 测试再次设置为 true
            service.SetSilentStart(true);
            Assert.True(service.GetSilentStart());
        }

        /// <summary>
        /// AlwaysRunAsAdmin 属性的读写应直接更新内存中的设置。
        /// </summary>
        [Fact]
        public void AlwaysRunAsAdmin_ShouldUpdateInMemoryValue()
        {
            var service = CreateServiceWithTempPaths(BuildConfiguration(), CreateTempRoot(), out _, out _, out _);

            Assert.False(service.GetAlwaysRunAsAdmin()); // 默认值
            service.SetAlwaysRunAsAdmin(true);
            Assert.True(service.GetAlwaysRunAsAdmin());
            service.SetAlwaysRunAsAdmin(false);
            Assert.False(service.GetAlwaysRunAsAdmin());
        }

        /// <summary>
        /// WindowPosition 属性的读写应直接更新内存中的设置。
        /// </summary>
        [Fact]
        public void WindowPosition_ShouldUpdateInMemoryValue()
        {
            var service = CreateServiceWithTempPaths(BuildConfiguration(), CreateTempRoot(), out _, out _, out _);

            Assert.Equal(WindowPosition.FollowCaret, service.GetWindowPosition()); // 默认值
            service.SetWindowPosition(WindowPosition.ScreenCenter);
            Assert.Equal(WindowPosition.ScreenCenter, service.GetWindowPosition());
            service.SetWindowPosition(WindowPosition.FollowCaret);
            Assert.Equal(WindowPosition.FollowCaret, service.GetWindowPosition());
        }

        /// <summary>
        /// GetTheme 当值为 null 时应返回默认值 "System"。
        /// </summary>
        [Fact]
        public void GetTheme_ShouldReturnSystemWhenNull()
        {
            var service = CreateServiceWithTempPaths(BuildConfiguration(), CreateTempRoot(), out _, out _, out _);
            var settings = GetPrivateSettings(service);
            settings.Theme = null;

            Assert.Equal("System", service.GetTheme());
        }

        /// <summary>
        /// SetLogLevel 只有在值发生变化时才应更新日志级别。
        /// </summary>
        [Fact]
        public void SetLogLevel_ShouldOnlyUpdateWhenChanged()
        {
            var service = CreateServiceWithTempPaths(BuildConfiguration(), CreateTempRoot(), out _, out _, out var levelSwitch);

            // 先设置为已知状态
            service.SetLogLevel(LogEventLevel.Warning);
            Assert.Equal(LogEventLevel.Warning, levelSwitch.MinimumLevel);
            Assert.Equal(LogEventLevel.Warning, service.GetLogLevel());

            // 设置相同值，不应触发变化（验证内部状态一致）
            service.SetLogLevel(LogEventLevel.Warning);
            Assert.Equal(LogEventLevel.Warning, levelSwitch.MinimumLevel);

            // 设置不同值，应触发变化
            service.SetLogLevel(LogEventLevel.Debug);
            Assert.Equal(LogEventLevel.Debug, levelSwitch.MinimumLevel);
            Assert.Equal(LogEventLevel.Debug, service.GetLogLevel());

            // 再次设置相同值
            service.SetLogLevel(LogEventLevel.Debug);
            Assert.Equal(LogEventLevel.Debug, levelSwitch.MinimumLevel);
        }

        /// <summary>
        /// LoadAsync 当用户配置文件不存在时应使用默认设置。
        /// </summary>
        [Fact]
        public async Task LoadAsync_ShouldUseDefaultsWhenUserFileNotExists()    
        {
            var config = BuildConfiguration();
            var tempRoot = CreateTempRoot();
            var defaultSettingsPath = Path.Combine(tempRoot, "appsettings.json");
            var defaultJson = """
            {
              "HotKey": "Ctrl + `",
              "Theme": "System",
              "HistoryLimit": 500,
              "ClipboardItemMinHeight": 56,
              "ClipboardItemMaxHeight": 56
            }
            """;
            File.WriteAllText(defaultSettingsPath, defaultJson);
            var nonExistentPath = Path.Combine(tempRoot, "NonExistent", "settings.json");

            var service = CreateServiceWithTempPaths(config, tempRoot, out _, out _, out _, defaultSettingsPath, nonExistentPath);

            await service.LoadAsync();

            // 应使用默认值
            Assert.Equal("Ctrl + `", service.GetHotKey());
            Assert.Equal("System", service.GetTheme());
            Assert.Equal(500, service.GetHistoryLimit());
            Assert.Equal(56, service.GetClipboardItemMinHeight());
            Assert.Equal(56, service.GetClipboardItemMaxHeight());
        }

        /// <summary>
        /// LoadAsync 当配置文件包含无效 JSON 时应回退到默认设置。
        /// </summary>
        [Fact]
        public async Task LoadAsync_ShouldFallbackToDefaultsOnInvalidJson()     
        {
            var config = BuildConfiguration();
            var tempRoot = CreateTempRoot();
            var defaultSettingsPath = Path.Combine(tempRoot, "appsettings.json");
            var defaultJson = """
            {
              "HotKey": "Ctrl + `",
              "Theme": "System"
            }
            """;
            File.WriteAllText(defaultSettingsPath, defaultJson);
            var userDir = Path.Combine(tempRoot, "UserSettings");
            Directory.CreateDirectory(userDir);
            var userSettingsPath = Path.Combine(userDir, "settings.json");   

            // 写入无效 JSON
            File.WriteAllText(userSettingsPath, "{ invalid json }");

            var service = CreateServiceWithTempPaths(config, tempRoot, out _, out _, out _, defaultSettingsPath, userSettingsPath);

            await service.LoadAsync();

            // 应使用默认值
            Assert.Equal("Ctrl + `", service.GetHotKey());
            Assert.Equal("System", service.GetTheme());
        }

        /// <summary>
        /// SaveAsync 应在用户目录保存 SilentStart 和 AlwaysRunAsAdmin 设置。
        /// </summary>
        [Fact]
        public async Task SaveAsync_ShouldPersistSilentStartAndAlwaysRunAsAdmin()
        {
            var config = BuildConfiguration();
            var tempRoot = CreateTempRoot();
            var service = CreateServiceWithTempPaths(config, tempRoot, out _, out var settingsPath, out _);

            service.SetSilentStart(true);
            service.SetAlwaysRunAsAdmin(true);

            await service.SaveAsync();

            Assert.True(File.Exists(settingsPath));
            using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(settingsPath));
            var root = doc.RootElement;
            Assert.True(root.GetProperty("SilentStart").GetBoolean());
            Assert.True(root.GetProperty("AlwaysRunAsAdmin").GetBoolean());
        }

        /// <summary>
        /// SaveAsync 应保存 WindowPosition 设置。
        /// </summary>
        [Fact]
        public async Task SaveAsync_ShouldPersistWindowPosition()
        {
            var config = BuildConfiguration();
            var tempRoot = CreateTempRoot();
            var service = CreateServiceWithTempPaths(config, tempRoot, out _, out var settingsPath, out _);

            service.SetWindowPosition(WindowPosition.ScreenCenter);

            await service.SaveAsync();

            Assert.True(File.Exists(settingsPath));
            using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(settingsPath));
            var root = doc.RootElement;
            Assert.Equal("ScreenCenter", root.GetProperty("WindowPosition").GetString());
        }

        /// <summary>
        /// LoadAsync 应正确加载 SilentStart、AlwaysRunAsAdmin 和 WindowPosition。
        /// </summary>
        [Fact]
        public async Task LoadAsync_ShouldLoadSilentStartAlwaysRunAsAdminAndWindowPosition()
        {
            var config = BuildConfiguration();
            var tempRoot = CreateTempRoot();
            var userDir = Path.Combine(tempRoot, "UserSettings");
            Directory.CreateDirectory(userDir);
            var userSettingsPath = Path.Combine(userDir, "settings.json");

            var userJson = """
            {
              "SilentStart": true,
              "AlwaysRunAsAdmin": true,
              "WindowPosition": "ScreenCenter"
            }
            """;
            File.WriteAllText(userSettingsPath, userJson);

            var service = CreateServiceWithTempPaths(config, tempRoot, out _, out _, out _, null, userSettingsPath);

            await service.LoadAsync();

            Assert.True(service.GetSilentStart());
            Assert.True(service.GetAlwaysRunAsAdmin());
            Assert.Equal(WindowPosition.ScreenCenter, service.GetWindowPosition());
        }

        /// <summary>
        /// 构建带有可选覆盖项的 IConfiguration，默认使用内存集合避免依赖真实文件。
        /// </summary>
        private static IConfiguration BuildConfiguration(Dictionary<string, string?>? values = null)
        {
            var builder = new ConfigurationBuilder();
            if (values is not null)
            {
                builder.AddInMemoryCollection(values!);
            }

            return builder.Build();
        }

        /// <summary>
        /// 创建 SettingsService 并以反射注入临时路径，保证文件读写落在隔离目录。
        /// </summary>
        private SettingsService CreateServiceWithTempPaths(
            IConfiguration configuration,
            string tempRoot,
            out string userDirectory,
            out string settingsFilePath,
            out LoggingLevelSwitch loggingLevelSwitch,
            string? defaultSettingsPath = null,
            string? precreatedUserSettingsPath = null)
        {
            Directory.CreateDirectory(tempRoot);

            userDirectory = precreatedUserSettingsPath is not null
                ? Path.GetDirectoryName(precreatedUserSettingsPath)!
                : Path.Combine(tempRoot, "UserSettings");

            settingsFilePath = precreatedUserSettingsPath ?? Path.Combine(userDirectory, "settings.json");
            defaultSettingsPath ??= Path.Combine(tempRoot, "appsettings.json");

            loggingLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
            var service = new SettingsService(configuration, _loggerMock.Object, _autoStartServiceMock.Object, loggingLevelSwitch);

            OverridePrivateField(service, "_defaultSettingsFilePath", defaultSettingsPath);
            OverridePrivateField(service, "_userSettingsDirectory", userDirectory);
            OverridePrivateField(service, "_settingsFilePath", settingsFilePath);

            // 覆盖路径后需要重新初始化设置，因为构造函数已使用真实路径加载了设置
            ResetSettings(service);

            return service;
        }

        /// <summary>
        /// 便捷地覆盖 SettingsService 的私有字段，允许定制测试路径。
        /// </summary>
        private static void OverridePrivateField(SettingsService service, string fieldName, object value)
        {
            var field = typeof(SettingsService).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? throw new InvalidOperationException($"未找到字段 {fieldName}");
            field.SetValue(service, value);
        }

        /// <summary>
        /// 使用当前配置路径重新加载设置，清除构造函数加载的真实用户配置。
        /// </summary>
        private static void ResetSettings(SettingsService service)
        {
            var method = typeof(SettingsService).GetMethod("LoadSettings", BindingFlags.Instance | BindingFlags.NonPublic)
                         ?? throw new InvalidOperationException("未找到 LoadSettings 方法。");
            method.Invoke(service, null);
        }

        /// <summary>
        /// 读取 SettingsService 内部的 AppSettings，便于验证连接串等没有公开访问器的字段。
        /// </summary>
        private static AppSettings GetPrivateSettings(SettingsService service)
        {
            var field = typeof(SettingsService).GetField("_settings", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? throw new InvalidOperationException("未找到 _settings 字段。");
            return (AppSettings)field.GetValue(service)!;
        }

        /// <summary>
        /// 创建唯一的临时根目录，避免测试间路径冲突。
        /// </summary>
        private static string CreateTempRoot()
        {
            var path = Path.Combine(Path.GetTempPath(), "ClipMateTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }
    }
}

