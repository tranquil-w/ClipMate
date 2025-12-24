using ClipMate.UI.Bootstrap;
using Prism.Ioc;

namespace ClipMate.Composition;

internal static class ServiceModule
{
    internal static void RegisterServiceLayer(this IContainerRegistry containerRegistry)
    {
        SharedServiceRegistration.RegisterServiceLayer(new PrismServiceRegistryAdapter(containerRegistry));
    }
}
