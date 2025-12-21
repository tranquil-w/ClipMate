using System.Diagnostics;
using System.Windows;

namespace ClipMate.Tests.TestHelpers
{
    [Collection("AppFixture collection")]
    public class TestBase : IDisposable
    {
        public TestBase()
        {
            var message = $"Create test class '{this.GetType().Name}' with Thread.CurrentThread: {Environment.CurrentManagedThreadId}" +
                          $" and Current.Dispatcher.Thread: {Application.Current.Dispatcher.Thread.ManagedThreadId}";
            Debug.WriteLine(message);

            Application.Current.Dispatcher.Invoke(() =>
            {
                var windows = Application.Current.Windows.OfType<Window>().ToList();
                windows.ForEach(w => w.Hide());
            });
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public virtual void Dispose()
        {
            var message = $"Dispose test class '{this.GetType().Name}' with Thread.CurrentThread: {Environment.CurrentManagedThreadId}" +
                          $" and Current.Dispatcher.Thread: {Application.Current.Dispatcher.Thread.ManagedThreadId}";
            Debug.WriteLine(message);
        }
    }
}
