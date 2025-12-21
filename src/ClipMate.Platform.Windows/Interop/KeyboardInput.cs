using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ClipMate.Platform.Windows.Interop;

internal static partial class KeyboardInput
{
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_V = 0x56;
    private const ushort VK_MENU = 0x12;
    private const ushort VK_F24 = 0x87;
    private const ushort VK_RSHIFT = 0xA1;

    internal static void SendCtrlV()
    {
        var inputs = new[]
        {
            INPUT.KeyboardDown(VK_CONTROL),
            INPUT.KeyboardDown(VK_V),
            INPUT.KeyboardUp(VK_V),
            INPUT.KeyboardUp(VK_CONTROL)
        };

        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        if (sent == 0)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"SendInput 发送失败：error={error} ({new Win32Exception(error).Message})");
        }

        if (sent != inputs.Length)
            throw new InvalidOperationException($"SendInput 发送不完整：sent={sent}, expected={inputs.Length}");
    }

    internal static void SendAltTapBestEffort()
    {
        SendKeyTapBestEffort(VK_MENU);
    }

    internal static void SendF24TapBestEffort()
    {
        SendKeyTapBestEffort(VK_F24);
    }

    internal static void SendRightShiftTapBestEffort()
    {
        SendKeyTapBestEffort(VK_RSHIFT);
    }

    internal static void SendRightShiftKeyUpBestEffort()
    {
        TrySendKeyUp(INPUT.KeyboardUp(VK_RSHIFT));
    }

    private static void SendKeyTapBestEffort(ushort virtualKey)
    {
        var keyUpInput = INPUT.KeyboardUp(virtualKey);
        var needsCompensationKeyUp = true;

        try
        {
            var inputs = new[]
            {
                INPUT.KeyboardDown(virtualKey),
                keyUpInput
            };

            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
            if (sent == inputs.Length)
            {
                needsCompensationKeyUp = false;
                return;
            }

            if (sent == 0)
            {
                var error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException(
                    $"SendInput 发送失败：error={error} ({new Win32Exception(error).Message})");
            }
        }
        finally
        {
            if (needsCompensationKeyUp)
            {
                TrySendKeyUp(keyUpInput);
            }
        }
    }

    private static void TrySendKeyUp(INPUT keyUpInput)
    {
        try
        {
            _ = SendInput(1, new[] { keyUpInput }, Marshal.SizeOf<INPUT>());
        }
        catch
        {
            // best-effort: ignore
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, [In] INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;

        public static INPUT KeyboardDown(ushort virtualKey)
        {
            return new INPUT
            {
                type = INPUT_KEYBOARD,
                U = InputUnion.FromKeyboard(new KEYBDINPUT { wVk = virtualKey })
            };
        }

        public static INPUT KeyboardUp(ushort virtualKey)
        {
            return new INPUT
            {
                type = INPUT_KEYBOARD,
                U = InputUnion.FromKeyboard(new KEYBDINPUT { wVk = virtualKey, dwFlags = KEYEVENTF_KEYUP })
            };
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;

        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public HARDWAREINPUT hi;

        public static InputUnion FromKeyboard(KEYBDINPUT input)
        {
            return new InputUnion { ki = input };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public nuint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public nuint dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }
}
