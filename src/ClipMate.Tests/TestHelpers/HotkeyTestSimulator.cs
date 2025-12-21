using SharpHook.Data;
using SharpHook.Testing;

namespace ClipMate.Tests.TestHelpers
{
    /// <summary>
    /// 键盘钩子测试模拟器
    /// </summary>
    public static class HotkeyTestSimulator
    {
        /// <summary>
        /// 模拟按下并释放修饰键 + 主键的组合
        /// </summary>
        public static void SimulateHotkey(
            TestGlobalHook hook,
            KeyCode mainKey,
            bool ctrl = false,
            bool alt = false,
            bool shift = false,
            bool win = false)
        {
            // 按下修饰键
            if (ctrl) hook.SimulateKeyPress(KeyCode.VcLeftControl);
            if (alt) hook.SimulateKeyPress(KeyCode.VcLeftAlt);
            if (shift) hook.SimulateKeyPress(KeyCode.VcLeftShift);
            if (win) hook.SimulateKeyPress(KeyCode.VcLeftMeta);

            // 按下主键
            hook.SimulateKeyPress(mainKey);

            // 释放主键
            hook.SimulateKeyRelease(mainKey);

            // 释放修饰键
            if (win) hook.SimulateKeyRelease(KeyCode.VcLeftMeta);
            if (shift) hook.SimulateKeyRelease(KeyCode.VcLeftShift);
            if (alt) hook.SimulateKeyRelease(KeyCode.VcLeftAlt);
            if (ctrl) hook.SimulateKeyRelease(KeyCode.VcLeftControl);
        }
    }
}
