using ClipMate.Service.Interfaces;
using ClipMate.Services;
using ClipMate.Service.Windowing;
using Prism.Ioc;

namespace ClipMate.Composition;

internal static class AppServicesModule
{
    internal static void RegisterAppServices(this IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<IApplicationService, ApplicationService>();
        containerRegistry.RegisterSingleton<IThemeService, ThemeService>();
        containerRegistry.RegisterSingleton<ISettingsService, SettingsService>();
        containerRegistry.RegisterSingleton<IClipboardService, ClipboardService>();
        containerRegistry.RegisterSingleton<IAdminService, AdminService>();
        containerRegistry.RegisterSingleton<IMainWindowPositionService, MainWindowPositionService>();
        containerRegistry.RegisterSingleton<IHotkeyService, HotkeyServiceAdapter>();
        containerRegistry.RegisterSingleton<MainWindowOverlayService>();
        containerRegistry.RegisterSingleton<NotifyIconCommandHandler>();
    }
}
