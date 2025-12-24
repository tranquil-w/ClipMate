using ClipMate.Service.Interfaces;
using ClipMate.Avalonia.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Diagnostics;

namespace ClipMate.Avalonia.Services;

public sealed class NotifyIconCommandHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly ISettingsService _settingsService;
    private bool _isSettingsWindowOpen;

    public NotifyIconCommandHandler(
        IServiceProvider serviceProvider,
        ILogger logger,
        ISettingsService settingsService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _settingsService = settingsService;
    }

    public void OpenSettings()
    {
        if (_isSettingsWindowOpen)
        {
            _logger.Information("设置窗口已打开，忽略新的打开请求");
            return;
        }

        _logger.Debug("打开设置窗口");
        _isSettingsWindowOpen = true;

        var window = _serviceProvider.GetRequiredService<SettingsWindow>();
        window.Closed += (_, _) => _isSettingsWindowOpen = false;
        window.Show();
    }

    public void OpenUserFolder()
    {
        try
        {
            var userFolder = _settingsService.GetUserFolder();
            Process.Start(new ProcessStartInfo
            {
                FileName = userFolder,
                UseShellExecute = true
            });
            _logger.Information("打开用户文件夹: {Folder}", userFolder);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "打开用户文件夹时发生错误");
        }
    }
}
