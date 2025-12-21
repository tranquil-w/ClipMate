using ClipMate.Service.Interfaces;
using ClipMate.Platform.Abstractions.Clipboard;
using ClipMate.Service.Clipboard;
using ClipMate.Service.Infrastructure;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using SharpHook;
using Serilog;

namespace ClipMate.Tests.TestHelpers;

public static class TestServiceProviderFactory
{
    public static IConfiguration CreateConfiguration(string filePath = "appsettings.json")
    {
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // 设置基路径
            .AddJsonFile(filePath, optional: false, reloadOnChange: true); // 加载指定的JSON配置文件

        return configurationBuilder.Build();
    }

    public static ServiceProvider CreateServiceProvider(IConfiguration? configuration = null)
    {
        var serviceCollection = new ServiceCollection();

        // 如果没有传入 IConfiguration，使用默认的从文件构建
        configuration ??= CreateConfiguration();
        serviceCollection.AddSingleton(configuration);

        serviceCollection.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        serviceCollection.AddSingleton<IEventSimulator, EventSimulator>();
        serviceCollection.AddSingleton<ILogger>(_ => new LoggerConfiguration().MinimumLevel.Debug().CreateLogger());

        var connectionString = configuration.GetConnectionString("ClipMateDb")
                               ?? throw new InvalidOperationException("缺少连接字符串配置：ClipMateDb");
        serviceCollection.AddSingleton<ISqliteConnectionFactory>(_ => new SqliteConnectionFactory(connectionString));

        serviceCollection.AddSingleton<IClipboardChangeSource, FakeClipboardChangeSource>();
        serviceCollection.AddSingleton<IDatabaseService, DatabaseService>();
        serviceCollection.AddSingleton<IClipboardItemRepository, DatabaseClipboardItemRepository>();
        serviceCollection.AddSingleton<IClipboardCaptureUseCase, ClipboardCaptureUseCase>();
        serviceCollection.AddSingleton<IClipboardHistoryUseCase, ClipboardHistoryUseCase>();

        return serviceCollection.BuildServiceProvider();
    }
}
