using ClipMate.Service.Clipboard;
using ClipMate.Service.Interfaces;
using ClipMate.UI.Abstractions;
using ClipMate.ViewModels;
using Serilog;

namespace ClipMate.Avalonia.ViewModels;

public sealed class SettingsViewModel : SettingsViewModelBase
{
    public SettingsViewModel(
        ISettingsService settingsService,
        IThemeService themeService,
        IHotkeyService hotkeyService,
        ClipMate.Platform.Abstractions.Input.IKeyboardHook keyboardHook,
        ClipMate.Platform.Abstractions.Startup.IAutoStartService autoStartService,
        IAdminService adminService,
        IApplicationService applicationService,
        IClipboardHistoryUseCase clipboardHistoryUseCase,
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
    }
}
