namespace ClipMate.Service.Interfaces
{
    /// <summary>
    /// 窗口切换服务接口
    /// </summary>
    public interface IWindowSwitchService
    {
        /// <summary>
        /// 切换到要粘贴的窗口
        /// </summary>
        void SwitchToPastingWindow();

        /// <summary>
        /// 获取要粘贴的窗口句柄
        /// </summary>
        nint GetPastingWindow();
    }
}
