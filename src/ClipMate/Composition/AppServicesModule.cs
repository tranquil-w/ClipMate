using ClipMate.Service.Interfaces;
using ClipMate.Services;
using ClipMate.Service.Windowing;
using ClipMate.UI.Abstractions;
using ClipMate.UI.Bootstrap;
using Prism.Ioc;

namespace ClipMate.Composition;

internal static class AppServicesModule
{
    internal static void RegisterAppServices(this IContainerRegistry containerRegistry)
    {
        SharedServiceRegistration.RegisterSharedAppServices(new PrismServiceRegistryAdapter(containerRegistry));

        containerRegistry.RegisterSingleton<IApplicationService, ApplicationService>();
        containerRegistry.RegisterSingleton<IThemeService, ThemeService>();
        containerRegistry.RegisterSingleton<IClipboardService, ClipboardService>();
        containerRegistry.RegisterSingleton<IMainWindowPositionService, MainWindowPositionService>();
        containerRegistry.RegisterSingleton<IUiDispatcher, WpfUiDispatcher>();
        containerRegistry.RegisterSingleton<IUserDialogService, WpfUserDialogService>();
        containerRegistry.RegisterSingleton<MainWindowOverlayService>();
        containerRegistry.RegisterSingleton<NotifyIconCommandHandler>();
    }
}
