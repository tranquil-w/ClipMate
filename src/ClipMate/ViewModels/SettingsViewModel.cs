using ClipMate.Service.Interfaces;
using ClipMate.UI.Abstractions;
using Prism.Dialogs;
using Serilog;

namespace ClipMate.ViewModels;

/// <summary>
/// 设置视图模型，负责 Prism 对话框交互
/// </summary>
public sealed class SettingsViewModel : SettingsViewModelBase, IDialogAware
{
    public SettingsViewModel(
        ISettingsService settingsService,
        IThemeService themeService,
        IHotkeyService hotkeyService,
        ClipMate.Platform.Abstractions.Input.IKeyboardHook keyboardHook,
        ClipMate.Platform.Abstractions.Startup.IAutoStartService autoStartService,
        IAdminService adminService,
        IApplicationService applicationService,
        ClipMate.Service.Clipboard.IClipboardHistoryUseCase clipboardHistoryUseCase,
        IUserDialogService dialogService,
        IUiDispatcher uiDispatcher,
        ILogger logger)
        : base(
            settingsService,
            themeService,
            hotkeyService,
            keyboardHook,
            autoStartService,
            adminService,
            applicationService,
            clipboardHistoryUseCase,
            dialogService,
            uiDispatcher,
            logger)
    {
        CloseRequested += (_, _) => RequestClose.Invoke(ButtonResult.OK);
    }

    public string Title => "设置";

    public DialogCloseListener RequestClose { get; } = new DialogCloseListener();

    public bool CanCloseDialog()
    {
        return true;
    }

    public void OnDialogClosed()
    {
        Logger.Debug("设置对话框已关闭");
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        Logger.Debug("设置对话框已打开");
    }
}
