using ClipMate.Platform.Abstractions.Input;
using ClipMate.Platform.Windows.Input;
using Moq;
using Serilog;
using SharpHook.Data;
using SharpHook.Testing;
using PlatformKeyboardHookEventArgs = ClipMate.Platform.Abstractions.Input.KeyboardHookEventArgs;

namespace ClipMate.Tests.Services;

public class WindowsKeyboardHookTests
{
    private readonly Mock<ILogger> _loggerMock = new();
    private readonly TestGlobalHook _testHook = new();
    private readonly WindowsKeyboardHook _service;
    private int _winComboGuardInjections;

    public WindowsKeyboardHookTests()
    {
        _service = new WindowsKeyboardHook(
            _loggerMock.Object,
            () => _testHook,
            injectWinComboGuard: () => _winComboGuardInjections++);
    }

    [Fact]
    public void Start_WhenNotStarted_ShouldRunHook()
    {
        _service.Start();

        Assert.True(_testHook.IsRunning);
    }

    [Fact]
    public void Stop_WhenRunning_ShouldDisposeHook()
    {
        _service.Start();

        _service.Stop();

        Assert.True(_testHook.IsDisposed);
    }

    [Fact]
    public void KeyPressed_ShouldMapKeyAndModifiers()
    {
        _service.Start();

        PlatformKeyboardHookEventArgs? received = null;
        _service.KeyPressed += (_, e) => received = e;

        _testHook.SimulateKeyPress(KeyCode.VcLeftControl);
        _testHook.SimulateKeyPress(KeyCode.VcLeftShift);
        _testHook.SimulateKeyPress(KeyCode.VcV);

        Assert.NotNull(received);
        Assert.Equal(VirtualKey.V, received!.Key);
        Assert.True(received.Modifiers.HasFlag(KeyModifiers.Ctrl));
        Assert.True(received.Modifiers.HasFlag(KeyModifiers.Shift));
    }

    [Fact]
    public void Suppress_WhenWinV_ShouldSuppressVKeyDownAndKeyUp_AndInjectGuardOnVReleaseWhileWinDown()
    {
        _service.Start();

        _service.KeyPressed += (_, e) =>
        {
            if (e.Key == VirtualKey.V && e.Modifiers.HasFlag(KeyModifiers.Win))
            {
                e.Suppress = true;
            }
        };

        var suppressedVKeyDown = 0;
        _testHook.KeyPressed += (_, e) =>
        {
            if (e.Data.KeyCode == KeyCode.VcV && e.SuppressEvent)
            {
                suppressedVKeyDown++;
            }
        };

        var suppressedVKeyUp = 0;
        _testHook.KeyReleased += (_, e) =>
        {
            if (e.Data.KeyCode == KeyCode.VcV && e.SuppressEvent)
            {
                suppressedVKeyUp++;
            }
        };

        _testHook.SimulateKeyPress(KeyCode.VcLeftMeta);
        _testHook.SimulateKeyPress(KeyCode.VcV);
        _testHook.SimulateKeyRelease(KeyCode.VcV);
        _testHook.SimulateKeyRelease(KeyCode.VcLeftMeta);

        Assert.Equal(1, suppressedVKeyDown);
        Assert.Equal(1, suppressedVKeyUp);
        Assert.Equal(1, _winComboGuardInjections);
    }

    [Fact]
    public void Suppress_WhenWinVAndWinReleasedFirst_ShouldInjectGuardOnWinRelease()
    {
        _service.Start();

        _service.KeyPressed += (_, e) =>
        {
            if (e.Key == VirtualKey.V && e.Modifiers.HasFlag(KeyModifiers.Win))
            {
                e.Suppress = true;
            }
        };

        _testHook.SimulateKeyPress(KeyCode.VcLeftMeta);
        _testHook.SimulateKeyPress(KeyCode.VcV);
        _testHook.SimulateKeyRelease(KeyCode.VcLeftMeta);
        _testHook.SimulateKeyRelease(KeyCode.VcV);

        Assert.Equal(1, _winComboGuardInjections);
    }

    [Fact]
    public void Suppress_WhenWinVAndGuardDisabled_ShouldNotInjectGuard()
    {
        _service.EnableWinComboGuardInjection = false;
        _service.Start();

        _service.KeyPressed += (_, e) =>
        {
            if (e.Key == VirtualKey.V && e.Modifiers.HasFlag(KeyModifiers.Win))
            {
                e.Suppress = true;
            }
        };

        _testHook.SimulateKeyPress(KeyCode.VcLeftMeta);
        _testHook.SimulateKeyPress(KeyCode.VcV);
        _testHook.SimulateKeyRelease(KeyCode.VcV);
        _testHook.SimulateKeyRelease(KeyCode.VcLeftMeta);

        Assert.Equal(0, _winComboGuardInjections);
    }
}
