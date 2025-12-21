namespace ClipMate.Service.Interfaces
{
    /// <summary>
    /// 快捷键服务接口，用于管理全局快捷键
    /// </summary>
    public interface IHotkeyService
    {
        /// <summary>
        /// 注册快捷键
        /// </summary>
        bool RegisterHotKey(string hotKey, Action callback);

        /// <summary>
        /// 取消注册快捷键
        /// </summary>
        bool UnregisterHotKey(string hotKey);

        /// <summary>
        /// 检查快捷键是否可用
        /// </summary>
        bool IsHotKeyAvailable(string hotKey);

        /// <summary>
        /// 获取已注册的所有快捷键
        /// </summary>
        IEnumerable<string> GetRegisteredHotKeys();

        /// <summary>
        /// 清除所有已注册的快捷键
        /// </summary>
        void ClearAllHotKeys();

        /// <summary>
        /// 注册主窗口切换快捷键（从设置中读取，如果为空则使用默认快捷键）
        /// </summary>
        /// <param name="toggleCallback">切换窗口的回调方法</param>
        /// <returns>注册是否成功</returns>
        bool RegisterMainWindowToggleHotkey(Action toggleCallback);

        /// <summary>
        /// 快捷键触发事件
        /// </summary>
        event EventHandler<string>? HotKeyPressed;
    }
}
