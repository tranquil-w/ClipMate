using ClipMate.Infrastructure;
using ClipMate.ViewModels;
using ClipMate.Views;
using Prism.Ioc;
using Prism.Navigation.Regions;

namespace ClipMate.Composition;

internal static class PresentationModule
{
    internal static void RegisterPresentation(this IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<MainWindowViewModel>();
        containerRegistry.RegisterSingleton<ClipboardViewModel>();

        var regionManager = containerRegistry.GetContainer().Resolve<IRegionManager>();
        regionManager.RegisterViewWithRegion(RegionNames.MainRegion, typeof(ClipboardView));

        containerRegistry.RegisterForNavigation<ClipboardView>("ClipboardView");

        containerRegistry.RegisterDialogWindow<ClipMate.Windows.HandyDialogWindow>();
        containerRegistry.RegisterDialog<SettingsView, SettingsViewModel>("SettingsView");
    }
}
