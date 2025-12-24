using ClipMate.UI.Bootstrap;
using Microsoft.Extensions.DependencyInjection;

namespace ClipMate.Avalonia.Infrastructure;

internal sealed class ServiceCollectionRegistryAdapter : IAppServiceRegistry
{
    private readonly IServiceCollection _services;

    public ServiceCollectionRegistryAdapter(IServiceCollection services)
    {
        _services = services;
    }

    public void RegisterSingleton<TService, TImplementation>() where TImplementation : class, TService where TService : class
    {
        _services.AddSingleton<TService, TImplementation>();
    }

    public void RegisterSingleton<TService>() where TService : class
    {
        _services.AddSingleton<TService>();
    }

    public void RegisterInstance<TService>(TService instance) where TService : class
    {
        _services.AddSingleton(instance);
    }
}
