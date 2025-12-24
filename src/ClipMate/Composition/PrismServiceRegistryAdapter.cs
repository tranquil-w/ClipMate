using ClipMate.UI.Bootstrap;
using Prism.Ioc;

namespace ClipMate.Composition;

internal sealed class PrismServiceRegistryAdapter : IAppServiceRegistry
{
    private readonly IContainerRegistry _registry;

    public PrismServiceRegistryAdapter(IContainerRegistry registry)
    {
        _registry = registry;
    }

    public void RegisterSingleton<TService, TImplementation>() where TImplementation : class, TService where TService : class
    {
        _registry.RegisterSingleton<TService, TImplementation>();
    }

    public void RegisterSingleton<TService>() where TService : class
    {
        _registry.RegisterSingleton<TService>();
    }

    public void RegisterInstance<TService>(TService instance) where TService : class
    {
        _registry.RegisterInstance(instance);
    }
}
