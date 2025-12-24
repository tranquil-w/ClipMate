namespace ClipMate.UI.Bootstrap;

public interface IAppServiceRegistry
{
    void RegisterSingleton<TService, TImplementation>() where TImplementation : class, TService where TService : class;

    void RegisterSingleton<TService>() where TService : class;

    void RegisterInstance<TService>(TService instance) where TService : class;
}
