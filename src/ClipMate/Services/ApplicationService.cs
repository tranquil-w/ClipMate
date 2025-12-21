using ClipMate.Service.Interfaces;
using Serilog;
using System.Windows;

namespace ClipMate.Services;

/// <summary>
/// 应用程序服务实现
/// </summary>
public class ApplicationService : IApplicationService
{
    private readonly ILogger _logger;

    public ApplicationService(ILogger logger)
    {
        _logger = logger;
    }

    public void ToggleMainWindow()
    {
        // 通过 Application.Current 访问 App 实例
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
        Application.Current.Shutdown();
    }
}
