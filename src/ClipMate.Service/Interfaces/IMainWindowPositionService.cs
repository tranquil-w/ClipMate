namespace ClipMate.Service.Interfaces;

/// <summary>
/// 主窗口定位服务接口。
/// </summary>
public interface IMainWindowPositionService
{
    /// <summary>
    /// 根据设置与当前环境（光标/鼠标/屏幕工作区）定位主窗口。
    /// </summary>
    void PositionMainWindow();
}

