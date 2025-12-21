using ClipMate.Core.Models;
using ClipMate.Infrastructure;
using ClipMate.Service.Interfaces;
using ClipMate.Platform.Abstractions.Input;
using PlatformAutoStartMethod = ClipMate.Platform.Abstractions.Startup.AutoStartMethod;
using PlatformAutoStartService = ClipMate.Platform.Abstractions.Startup.IAutoStartService;
using ClipMate.Service.Clipboard;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyMessageBox = HandyControl.Controls.MessageBox;
using Serilog;
using Serilog.Events;
using System.Collections.Generic;
using System.Windows;

namespace ClipMate.ViewModels;

/// <summary>
/// 设置视图模型，管理应用程序的所有用户可配置选项
/// </summary>
/// <remarks>
/// 负责处理主题切换、快捷键设置、窗口位置、开机自启动、
/// 静默启动、管理员权限和日志级别等设置项的读取和保存。
/// </remarks>
public partial class SettingsViewModel : ObservableObject, IDialogAware
{
    private enum HotkeyRecordingTarget
    {
        None,
        MainWindow,
        FavoriteFilter
    }

    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private readonly IHotkeyService _hotkeyService;
    private readonly IKeyboardHook _keyboardHook;
    private readonly PlatformAutoStartService _autoStartService;
    private readonly IAdminService _adminService;
    private readonly IClipboardHistoryUseCase _clipboardHistoryUseCase;
    private readonly ILogger _logger;
    private bool _isInitializing = true;
    private EventHandler<KeyboardHookEventArgs>? _recordingHandler;
    private HotkeyRecordingTarget _recordingTarget = HotkeyRecordingTarget.None;

    [ObservableProperty]
    private string _currentHotKey = string.Empty;

    [ObservableProperty]
    private string _favoriteFilterHotKey = string.Empty;

    [ObservableProperty]
    private bool _isLightTheme;

    [ObservableProperty]
    private bool _isDarkTheme;

    [ObservableProperty]
    private bool _isSystemTheme;

    [ObservableProperty]
    private bool _isFollowCaret;

    [ObservableProperty]
    private bool _isFollowMouse;

    [ObservableProperty]
    private bool _isScreenCenter;

    [ObservableProperty]
    private bool _isRecordingHotKey;

    [ObservableProperty]
    private string _tempHotKey = string.Empty;

    [ObservableProperty]
    private bool _isRecordingFavoriteFilterHotKey;

    [ObservableProperty]
    private string _tempFavoriteFilterHotKey = string.Empty;

    [ObservableProperty]
    private string _version = string.Empty;

    [ObservableProperty]
    private int _clipboardItemMaxHeight = 100;

    [ObservableProperty]
    private int _historyLimit = 500;

    [ObservableProperty]
    private bool _isAutoStartEnabled;

    [ObservableProperty]
    private bool _isSilentStart;

    [ObservableProperty]
    private bool _isAlwaysRunAsAdmin;

    [ObservableProperty]
    private bool _isRunningAsAdmin;

    [ObservableProperty]
    private string _adminStatusText = string.Empty;

    [ObservableProperty]
    private string _autoStartMethodText = string.Empty;

    [ObservableProperty]
    private LogEventLevel _selectedLogLevel = LogEventLevel.Information;

    public IReadOnlyList<LogLevelOption> LogLevels { get; } = new List<LogLevelOption>
    {
        new("错误", LogEventLevel.Error),
        new("警告", LogEventLevel.Warning),
        new("信息", LogEventLevel.Information),
        new("调试", LogEventLevel.Debug)
    };

    public SettingsViewModel(
        ISettingsService settingsService,
        IThemeService themeService,
        IHotkeyService hotkeyService,
        IKeyboardHook keyboardHook,
        PlatformAutoStartService autoStartService,
        IAdminService adminService,
        IClipboardHistoryUseCase clipboardHistoryUseCase,
        ILogger logger)
    {
        _settingsService = settingsService;
        _themeService = themeService;
        _hotkeyService = hotkeyService;
        _keyboardHook = keyboardHook;
        _autoStartService = autoStartService;
        _adminService = adminService;
        _clipboardHistoryUseCase = clipboardHistoryUseCase;
        _logger = logger;

        // 初始化版本信息
        Version = AppVersionProvider.GetCurrentVersion();

        LoadCurrentSettings();
        _isInitializing = false;
    }

    private void LoadCurrentSettings()
    {
        try
        {
            // 加载快捷键设置
            var hotKey = _settingsService.GetHotKey();
            CurrentHotKey = hotKey ?? "未设置";

            var favoriteFilterHotKey = _settingsService.GetFavoriteFilterHotKey();
            FavoriteFilterHotKey = favoriteFilterHotKey ?? "未设置";

            // 加载主题设置
            var theme = _settingsService.GetTheme();
            switch (theme)
            {
                case "Light":
                    IsLightTheme = true;
                    break;
                case "Dark":
                    IsDarkTheme = true;
                    break;
                default:
                    IsSystemTheme = true;
                    break;
            }

            // 加载窗口位置设置
            var windowPosition = _settingsService.GetWindowPosition();
            switch (windowPosition)
            {
                case WindowPosition.FollowMouse:
                    IsFollowMouse = true;
                    break;
                case WindowPosition.ScreenCenter:
                    IsScreenCenter = true;
                    break;
                default:
                    IsFollowCaret = true;
                    break;
            }

            // 加载剪贴项最大高度设置
            ClipboardItemMaxHeight = _settingsService.GetClipboardItemMaxHeight();

            // 加载历史记录上限设置
            HistoryLimit = _settingsService.GetHistoryLimit();

            // 加载开机自启动设置
            IsAutoStartEnabled = _settingsService.GetAutoStart();

            // 加载静默启动设置
            IsSilentStart = _settingsService.GetSilentStart();

            // 加载始终以管理员身份运行设置
            IsAlwaysRunAsAdmin = _settingsService.GetAlwaysRunAsAdmin();

            SelectedLogLevel = LogLevelPolicy.Normalize(_settingsService.GetLogLevel());

            // 更新管理员状态
            UpdateAdminStatus();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "加载设置时发生错误");
        }
    }

    [RelayCommand]
    private void RecordHotKey()
    {
        ToggleHotkeyRecording(
            HotkeyRecordingTarget.MainWindow,
            "开始录制快捷键（使用低级钩子）",
            "停止录制快捷键");
    }

    [RelayCommand]
    private void RecordFavoriteFilterHotKey()
    {
        ToggleHotkeyRecording(
            HotkeyRecordingTarget.FavoriteFilter,
            "开始录制收藏筛选快捷键（使用低级钩子）",
            "停止录制收藏筛选快捷键");
    }

    private void ToggleHotkeyRecording(HotkeyRecordingTarget target, string startLog, string stopLog)
    {
        if (_recordingTarget == target)
        {
            StopHotkeyRecording();
            SetRecordingState(target, false);
            _logger.Debug(stopLog);
            return;
        }

        StopHotkeyRecording();
        SetRecordingState(HotkeyRecordingTarget.MainWindow, false);
        SetRecordingState(HotkeyRecordingTarget.FavoriteFilter, false);
        SetRecordingState(target, true);
        SetTempHotkey(target, "按下要设置的快捷键...");
        StartHotkeyRecording(target);
        _logger.Debug(startLog);
    }

    private void StartHotkeyRecording(HotkeyRecordingTarget target)
    {
        StopHotkeyRecording();
        _recordingTarget = target;

        _keyboardHook.Start();
        _recordingHandler = (_, e) =>
        {
            if (_recordingTarget == HotkeyRecordingTarget.None)
            {
                return;
            }

            if (e.Modifiers == KeyModifiers.None)
            {
                return;
            }

            e.Suppress = true;

            var descriptor = new HotkeyDescriptor(e.Key, e.Modifiers);
            var recordingTarget = _recordingTarget;
            _ = Application.Current.Dispatcher.InvokeAsync(() => OnHotkeyRecorded(descriptor.DisplayString, recordingTarget));
        };

        _keyboardHook.KeyPressed += _recordingHandler;
    }

    private void StopHotkeyRecording()
    {
        if (_recordingHandler == null)
        {
            return;
        }

        _keyboardHook.KeyPressed -= _recordingHandler;
        _recordingHandler = null;
        _recordingTarget = HotkeyRecordingTarget.None;
    }

    private void OnHotkeyRecorded(string hotkeyString, HotkeyRecordingTarget target)
    {
        SetTempHotkey(target, hotkeyString);

        // 停止录制并保存快捷键
        StopHotkeyRecording();
        SetRecordingState(target, false);

        _ = Task.Run(async () => await ApplyRecordedHotkeyAsync(target, hotkeyString));
    }

    private async Task ApplyRecordedHotkeyAsync(HotkeyRecordingTarget target, string hotkeyString)
    {
        try
        {
            switch (target)
            {
                case HotkeyRecordingTarget.MainWindow:
                    CurrentHotKey = hotkeyString;
                    _settingsService.SetHotKey(CurrentHotKey);
                    await _settingsService.SaveAsync();
                    System.Windows.Application.Current.Dispatcher.Invoke(RegisterHotkeyFromSettings);
                    break;
                case HotkeyRecordingTarget.FavoriteFilter:
                    FavoriteFilterHotKey = hotkeyString;
                    _settingsService.SetFavoriteFilterHotKey(FavoriteFilterHotKey);
                    await _settingsService.SaveAsync();
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "保存快捷键时发生错误");
        }
    }

    private void SetRecordingState(HotkeyRecordingTarget target, bool isRecording)
    {
        switch (target)
        {
            case HotkeyRecordingTarget.MainWindow:
                IsRecordingHotKey = isRecording;
                break;
            case HotkeyRecordingTarget.FavoriteFilter:
                IsRecordingFavoriteFilterHotKey = isRecording;
                break;
        }
    }

    private void SetTempHotkey(HotkeyRecordingTarget target, string value)
    {
        switch (target)
        {
            case HotkeyRecordingTarget.MainWindow:
                TempHotKey = value;
                break;
            case HotkeyRecordingTarget.FavoriteFilter:
                TempFavoriteFilterHotKey = value;
                break;
        }
    }

    [RelayCommand]
    private void Close()
    {
        // 调用 RequestClose 关闭对话框
        RequestClose.Invoke(ButtonResult.OK);
        _logger.Debug("关闭设置对话框");
    }

    [RelayCommand]
    private void SelectLightTheme()
    {
        IsLightTheme = true;
    }

    [RelayCommand]
    private void SelectDarkTheme()
    {
        IsDarkTheme = true;
    }

    [RelayCommand]
    private void SelectSystemTheme()
    {
        IsSystemTheme = true;
    }

    [RelayCommand]
    private void SelectFollowCaret()
    {
        IsFollowCaret = true;
    }

    [RelayCommand]
    private void SelectFollowMouse()
    {
        IsFollowMouse = true;
    }

    [RelayCommand]
    private void SelectScreenCenter()
    {
        IsScreenCenter = true;
    }

    [RelayCommand]
    private void RestartAsAdmin()
    {
        var result = HandyMessageBox.Show(
            "需要重新启动以获取管理员权限，确认以管理员权限重启 ClipMate 吗？",
            "确认重启",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var restarted = _adminService.RestartAsAdministrator();
            if (restarted)
            {
                _logger.Information("正在以管理员权限重启应用");
                Application.Current.Shutdown();
            }
            else
            {
                _logger.Information("用户取消了管理员权限提升请求");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "以管理员权限重启时发生错误");
            HandyMessageBox.Show(
                "无法以管理员权限重启应用，请手动右键以管理员身份运行。",
                "操作失败",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    #region Property Change Handlers

    partial void OnIsLightThemeChanged(bool value)
    {
        if (_isInitializing || !value) return;

        IsDarkTheme = false;
        IsSystemTheme = false;
        ApplyThemeChange("Light");
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        if (_isInitializing || !value) return;

        IsLightTheme = false;
        IsSystemTheme = false;
        ApplyThemeChange("Dark");
    }

    partial void OnIsSystemThemeChanged(bool value)
    {
        if (_isInitializing || !value) return;

        IsLightTheme = false;
        IsDarkTheme = false;
        ApplyThemeChange("System");
    }

    partial void OnIsFollowCaretChanged(bool value)
    {
        if (_isInitializing || !value) return;

        IsFollowMouse = false;
        IsScreenCenter = false;
        ApplyWindowPositionChange(WindowPosition.FollowCaret);
    }

    partial void OnIsFollowMouseChanged(bool value)
    {
        if (_isInitializing || !value) return;

        IsFollowCaret = false;
        IsScreenCenter = false;
        ApplyWindowPositionChange(WindowPosition.FollowMouse);
    }

    partial void OnIsScreenCenterChanged(bool value)
    {
        if (_isInitializing || !value) return;

        IsFollowCaret = false;
        IsFollowMouse = false;
        ApplyWindowPositionChange(WindowPosition.ScreenCenter);
    }

    partial void OnClipboardItemMaxHeightChanged(int value)
    {
        if (_isInitializing) return;

        ApplyClipboardItemMaxHeightChange();
    }

    partial void OnHistoryLimitChanged(int value)
    {
        if (_isInitializing) return;

        ApplyHistoryLimitChange();
    }

    partial void OnIsAutoStartEnabledChanged(bool value)
    {
        if (_isInitializing) return;

        ApplyAutoStartChange();
    }

    partial void OnIsSilentStartChanged(bool value)
    {
        if (_isInitializing) return;

        _ = ApplySilentStartChange();
    }

    partial void OnIsAlwaysRunAsAdminChanged(bool value)
    {
        if (_isInitializing) return;

        ApplyAlwaysRunAsAdminChange();
    }

    partial void OnSelectedLogLevelChanged(LogEventLevel value)
    {
        if (_isInitializing) return;

        ApplyLogLevelChange();
    }

    #endregion

    /// <summary>
    /// 通用的设置变更应用方法，封装异步保存和错误处理逻辑
    /// </summary>
    /// <param name="settingAction">设置操作（在后台线程执行）</param>
    /// <param name="errorMessage">错误日志消息</param>
    /// <param name="uiCallback">可选的 UI 线程回调</param>
    /// <param name="additionalWorkAsync">可选的额外异步操作</param>
    private void ApplySettingChangeAsync(
        Action settingAction,
        string errorMessage,
        Action? uiCallback = null,
        Func<Task>? additionalWorkAsync = null)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                settingAction();
                await _settingsService.SaveAsync();

                if (additionalWorkAsync != null)
                    await additionalWorkAsync();

                if (uiCallback != null)
                    Application.Current.Dispatcher.Invoke(uiCallback);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, errorMessage);
            }
        });
    }

    private void ApplyThemeChange(string theme)
    {
        ApplySettingChangeAsync(
            () => _settingsService.SetTheme(theme),
            "立即应用主题时发生错误",
            () => _themeService.ApplyTheme(theme));
    }

    private void ApplyClipboardItemMaxHeightChange()
    {
        ApplySettingChangeAsync(
            () => _settingsService.SetClipboardItemMaxHeight(ClipboardItemMaxHeight),
            "保存剪贴项最大高度时发生错误");
    }

    private void ApplyHistoryLimitChange()
    {
        ApplySettingChangeAsync(
            () => _settingsService.SetHistoryLimit(HistoryLimit),
            "保存历史记录上限时发生错误",
            additionalWorkAsync: async () =>
            {
                var deletedCount = await _clipboardHistoryUseCase.CleanupOldItemsAsync(HistoryLimit);
                if (deletedCount > 0)
                {
                    _logger.Information("历史记录上限变更，清理了 {DeletedCount} 条记录", deletedCount);
                }
            });
    }

    private void ApplyWindowPositionChange(WindowPosition position)
    {
        ApplySettingChangeAsync(
            () => _settingsService.SetWindowPosition(position),
            "保存窗口显示位置时发生错误");
    }

    private void ApplyAutoStartChange()
    {
        ApplySettingChangeAsync(
            () => _settingsService.SetAutoStart(IsAutoStartEnabled),
            "保存开机自启动设置时发生错误",
            () => AutoStartMethodText = BuildAutoStartMethodText());
    }

    private async Task ApplySilentStartChange()
    {
        try
        {
            _settingsService.SetSilentStart(IsSilentStart);
            await _settingsService.SaveAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "保存静默启动设置时发生错误");
        }
    }

    private void ApplyAlwaysRunAsAdminChange()
    {
        ApplySettingChangeAsync(
            () => _settingsService.SetAlwaysRunAsAdmin(IsAlwaysRunAsAdmin),
            "保存始终以管理员身份运行设置时发生错误");
    }

    private void ApplyLogLevelChange()
    {
        ApplySettingChangeAsync(
            () => _settingsService.SetLogLevel(SelectedLogLevel),
            "更新日志级别时发生错误",
            additionalWorkAsync: () =>
            {
                _logger.Information("日志级别已更新为 {LogLevel}", SelectedLogLevel);
                return Task.CompletedTask;
            });
    }

    private void UpdateAdminStatus()
    {
        try
        {
            IsRunningAsAdmin = _adminService.IsRunningAsAdministrator();
            AdminStatusText = IsRunningAsAdmin ? "正在以管理员权限运行" : "当前未以管理员权限运行";
            AutoStartMethodText = BuildAutoStartMethodText();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "更新管理员状态时发生错误");
            AutoStartMethodText = "自启动方式：未知";
        }
    }

    private string BuildAutoStartMethodText()
    {
        var method = _autoStartService.GetCurrentMethod();
        return method switch
        {
            PlatformAutoStartMethod.TaskScheduler => "自启动方式：任务计划程序（管理员）",
            PlatformAutoStartMethod.Registry => "自启动方式：注册表（当前用户）",
            _ => "自启动方式：未启用"
        };
    }

    private void RegisterHotkeyFromSettings()
    {
        try
        {
            // 先取消注册所有现有的快捷键
            _hotkeyService.ClearAllHotKeys();

            var currentHotkey = _settingsService.GetHotKey();

            if (IsWinVHotkey(currentHotkey))
            {
                // Win+V 由 App 的键盘钩子拦截逻辑处理（此处只需清理 NHotkey 注册）
                _logger.Information("Win+V 快捷键已切换为键盘钩子拦截模式");
            }
            else
            {
                // 其他快捷键使用 NHotkey 处理
                var success = _hotkeyService.RegisterMainWindowToggleHotkey(App.Current.ToggleMainWindow);
                if (success)
                {
                    _logger.Information("主窗口切换快捷键注册成功");
                }
                else
                {
                    _logger.Warning("主窗口切换快捷键注册失败");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "注册快捷键时发生错误");
        }
    }

    /// <summary>
    /// 判断快捷键是否为 Win+V
    /// </summary>
    private static bool IsWinVHotkey(string? hotkey)
    {
        if (string.IsNullOrEmpty(hotkey)) return false;
        var normalized = hotkey.ToLowerInvariant().Replace(" ", "").Replace("+", "");
        return normalized == "winv" || normalized == "windowsv";
    }

    #region IDialogAware Implementation

    public string Title => "设置";

    public DialogCloseListener RequestClose { get; } = new DialogCloseListener();

    public bool CanCloseDialog()
    {
        return true;
    }

    public void OnDialogClosed()
    {
        _logger.Debug("设置对话框已关闭");
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        _logger.Debug("设置对话框已打开");
    }

    #endregion
}

public record LogLevelOption(string DisplayName, LogEventLevel Level);
