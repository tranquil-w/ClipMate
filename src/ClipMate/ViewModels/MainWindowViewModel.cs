using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace ClipMate.ViewModels;

/// <summary>
/// 主窗口ViewModel，负责管理导航和窗口行为
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    /// <summary>
    /// 关闭窗口命令（处理Escape键）
    /// </summary>
    [RelayCommand]
    private void CloseWindow()
    {
        Application.Current.MainWindow?.Close();
    }
}
