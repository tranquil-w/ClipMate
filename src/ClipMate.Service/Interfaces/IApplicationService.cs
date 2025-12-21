namespace ClipMate.Service.Interfaces;

/// <summary>
/// 应用程序级别的服务接口
/// </summary>
public interface IApplicationService
{
    /// <summary>
    /// 切换主窗口的显示/隐藏状态
    /// </summary>
    void ToggleMainWindow();

    /// <summary>
    /// 退出应用程序
    /// </summary>
    void Shutdown();
}
