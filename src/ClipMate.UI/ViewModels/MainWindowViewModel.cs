using ClipMate.Platform.Abstractions.Window;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClipMate.ViewModels;

/// <summary>
/// 主窗口ViewModel，负责管理导航和窗口行为
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    private readonly IMainWindowController _mainWindowController;

    public MainWindowViewModel(IMainWindowController mainWindowController)
    {
        _mainWindowController = mainWindowController;
    }

    /// <summary>
    /// 关闭窗口命令（处理Escape键）
    /// </summary>
    [RelayCommand]
    private void CloseWindow()
    {
        _mainWindowController.CloseMainWindow();
    }
}
