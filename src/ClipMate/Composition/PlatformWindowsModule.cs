using ClipMate.Platform.Abstractions.Clipboard;
using ClipMate.Platform.Abstractions.Input;
using ClipMate.Platform.Abstractions.Startup;
using ClipMate.Platform.Abstractions.Tray;
using ClipMate.Platform.Abstractions.Window;
using ClipMate.Platform.Windows.Clipboard;
using ClipMate.Platform.Windows.Input;
using ClipMate.Platform.Windows.Startup;
using ClipMate.Platform.Windows.Tray;
using ClipMate.Platform.Windows.Windowing;
using Prism.Ioc;
using SharpHook;

namespace ClipMate.Composition;

internal static class PlatformWindowsModule
{
    internal static void RegisterPlatformWindows(this IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<IEventSimulator, EventSimulator>();
        containerRegistry.RegisterInstance<Func<IGlobalHook>>(
            () => new SimpleGlobalHook(SharpHook.Data.GlobalHookType.Keyboard, null, runAsyncOnBackgroundThread: true));

        containerRegistry.RegisterSingleton<IClipboardChangeSource, WindowsClipboardChangeSource>();
        containerRegistry.RegisterSingleton<IClipboardWriter, WindowsClipboardWriter>();
        containerRegistry.RegisterSingleton<IPasteTrigger, WindowsPasteTrigger>();

        containerRegistry.RegisterSingleton<IMainWindowController, WpfMainWindowController>();
        containerRegistry.RegisterSingleton<IWindowPositionProvider, WindowsWindowPositionProvider>();
        containerRegistry.RegisterSingleton<IForegroundWindowService, WindowsForegroundWindowService>();
        containerRegistry.RegisterSingleton<IForegroundWindowTracker, WindowsForegroundWindowTracker>();

        containerRegistry.RegisterSingleton<IGlobalHotkeyService, WindowsGlobalHotkeyService>();
        containerRegistry.RegisterSingleton<IKeyboardHook, WindowsKeyboardHook>();

        containerRegistry.RegisterSingleton<IAutoStartService, WindowsAutoStartService>();

        containerRegistry.RegisterSingleton<ITrayIcon, WindowsTrayIcon>();
    }
}

