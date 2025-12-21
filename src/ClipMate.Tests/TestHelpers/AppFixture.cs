using System.Windows;
using System.Windows.Threading;

namespace ClipMate.Tests.TestHelpers;

public class AppFixture : IDisposable
{
    public AppFixture()
    {
        // ... initialize
        TestHost.Initialize();
    }

    public void Dispose()
    {
        // ... clean up
        GC.Collect();
        Dispatcher.ExitAllFrames();
        Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
    }
}

[CollectionDefinition("AppFixture collection")]
public class AppFixtureCollection : ICollectionFixture<AppFixture>
{
    // 这个集合定义类没有代码，也不会被实际实例化，它存在的意义仅仅是提供集合定义的信息。
}
