using ClipMate.Service.Interfaces;
using Prism.Dialogs;
using Serilog;
using System.Diagnostics;

namespace ClipMate.Services;

public sealed class NotifyIconCommandHandler
{
    private readonly IDialogService _dialogService;
    private readonly ILogger _logger;
    private readonly ISettingsService _settingsService;
    private bool _isSettingsDialogOpen;

    public NotifyIconCommandHandler(
        IDialogService dialogService,
        ILogger logger,
        ISettingsService settingsService)
    {
        _dialogService = dialogService;
        _logger = logger;
        _settingsService = settingsService;
    }

    public void OpenSettings()
    {
        if (_isSettingsDialogOpen)
        {
            _logger.Information("设置对话框已打开，忽略新的打开请求");
            return;
        }

        _logger.Debug("打开设置对话框");
        _isSettingsDialogOpen = true;

        try
        {
            _dialogService.ShowDialog("SettingsView");
        }
        finally
        {
            _isSettingsDialogOpen = false;
        }
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

