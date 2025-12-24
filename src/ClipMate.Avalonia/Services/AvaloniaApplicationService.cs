using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ClipMate.Service.Interfaces;
using Serilog;

namespace ClipMate.Avalonia.Services;

public sealed class AvaloniaApplicationService : IApplicationService
{
    private readonly ILogger _logger;

    public AvaloniaApplicationService(ILogger logger)
    {
        _logger = logger;
    }

    public void ToggleMainWindow()
    {
        if (Application.Current is App app)
        {
            _logger.Debug("切换主窗口显示状态");
            app.ToggleMainWindow();
        }
        else
        {
            _logger.Warning("无法获取 App 实例");
        }
    }

    public void Shutdown()
    {
        _logger.Information("用户请求退出应用");
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
    }
}
