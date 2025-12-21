// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// https://github.com/MahApps/MahApps.Metro.git

using System.Diagnostics;
using System.Windows;

namespace ClipMate.Tests.TestHelpers
{
    /// <summary>
    /// This class is the ultimate hack to work around that we can't 
    /// create more than one application in the same AppDomain
    /// 
    /// It is initialized once at startup and is never properly cleaned up, 
    /// this means the AppDomain will throw an exception when xUnit unloads it.
    /// 
    /// Your test runner will inevitably hate you and hang endlessly after every test has run.
    /// The Resharper runner will also throw an exception message in your face.
    /// 
    /// Better than no unit tests.
    /// </summary>
    public class TestHost
    {
        private TestApp? _app;
        private readonly Thread? _appThread;
        private readonly AutoResetEvent _gate = new(false);

        private static TestHost? _testHost;

        public static TestHost Instance => _testHost ?? throw new InvalidOperationException($"{nameof(TestHost)} is not initialized!");
        public static TestApp App => Instance._app ?? throw new InvalidOperationException($"{nameof(TestHost)} is not initialized!");
        public static IContainerProvider Container => App.Container;

        public static void Initialize()
        {
            _testHost ??= new TestHost();
        }

        private TestHost()
        {
            _appThread = new Thread(StartDispatcher);
            _appThread.SetApartmentState(ApartmentState.STA);
            _appThread.Start();

            _gate.WaitOne();
        }

        private void StartDispatcher()
        {
            _app = new TestApp { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            //this._app.InitializeComponent();
            _app.Exit += (_, _) =>
            {
                var message = $"Exit TestApp with Thread.CurrentThread: {Environment.CurrentManagedThreadId}" +
                              $" and Current.Dispatcher.Thread: {Application.Current.Dispatcher.Thread.ManagedThreadId}";
                Debug.WriteLine(message);
            };
            _app.Startup += async (_, _) =>
            {
                var message = $"Start TestApp with Thread.CurrentThread: {Environment.CurrentManagedThreadId}" +
                              $" and Current.Dispatcher.Thread: {Application.Current.Dispatcher.Thread.ManagedThreadId}";
                Debug.WriteLine(message);
                _gate.Set();
                await Task.Yield();
            };
            _app.Run();
        }

        /// <summary>
        /// Await this method in every test that should run on the UI thread.
        /// </summary>
        public static SwitchContextToUiThreadAwaiter SwitchToAppThread()
        {
            if (_testHost?._app is null)
            {
                throw new InvalidOperationException($"{nameof(TestHost)} is not initialized!");
            }

            return new SwitchContextToUiThreadAwaiter(_testHost._app.Dispatcher);
        }
    }
}