using System;
using ClipMate.Service.Interfaces;
using ClipMate.Services;
using Moq;
using Serilog;
using ClipMate.Platform.Abstractions.Input;

namespace ClipMate.Tests.Services
{
    /// <summary>
    /// HotkeyServiceAdapter 的单元测试，覆盖解析、注册/注销和可用性检查等关键路径。
    /// </summary>
    public class HotkeyServiceTests
    {
        private readonly Mock<ILogger> _loggerMock = new();
        private readonly Mock<ISettingsService> _settingsServiceMock = new();
        private readonly Mock<IGlobalHotkeyService> _globalHotkeyServiceMock = new();
        private readonly HotkeyServiceAdapter _service;

        public HotkeyServiceTests()
        {
            _service = new HotkeyServiceAdapter(_loggerMock.Object, _settingsServiceMock.Object, _globalHotkeyServiceMock.Object);
        }

        [Fact]
        public void RegisterHotKey_ValidCombination_ShouldParseAndForwardToGlobalService()
        {
            HotkeyDescriptor? received = null;
            _globalHotkeyServiceMock
                .Setup(s => s.Register(It.IsAny<HotkeyDescriptor>(), It.IsAny<Action>()))
                .Callback<HotkeyDescriptor, Action>((descriptor, _) => received = descriptor)
                .Returns(true);

            var ok = _service.RegisterHotKey("Ctrl+Shift+V", () => { });

            Assert.True(ok);
            Assert.Equal(new HotkeyDescriptor(VirtualKey.V, KeyModifiers.Ctrl | KeyModifiers.Shift), received);
        }

        [Fact]
        public void RegisterHotKey_BacktickAlias_ShouldMapToBackQuote()
        {
            HotkeyDescriptor? received = null;
            _globalHotkeyServiceMock
                .Setup(s => s.Register(It.IsAny<HotkeyDescriptor>(), It.IsAny<Action>()))
                .Callback<HotkeyDescriptor, Action>((descriptor, _) => received = descriptor)
                .Returns(true);

            var ok = _service.RegisterHotKey("Ctrl+`", () => { });

            Assert.True(ok);
            Assert.Equal(new HotkeyDescriptor(VirtualKey.BackQuote, KeyModifiers.Ctrl), received);
        }

        [Fact]
        public void RegisterHotKey_InvalidInput_ShouldReturnFalse()
        {
            var ok = _service.RegisterHotKey("Ctrl+NotAKey", () => { });

            Assert.False(ok);
            _globalHotkeyServiceMock.Verify(s => s.Register(It.IsAny<HotkeyDescriptor>(), It.IsAny<Action>()), Times.Never);
        }

        [Fact]
        public void RegisterAndUnregisterHotKey_ShouldTrackLifecycle()
        {
            var registered = new HashSet<HotkeyDescriptor>();

            _globalHotkeyServiceMock
                .Setup(s => s.Register(It.IsAny<HotkeyDescriptor>(), It.IsAny<Action>()))
                .Returns<HotkeyDescriptor, Action>((descriptor, _) => registered.Add(descriptor));

            _globalHotkeyServiceMock
                .Setup(s => s.Unregister(It.IsAny<HotkeyDescriptor>()))
                .Returns<HotkeyDescriptor>(descriptor => registered.Remove(descriptor));

            _globalHotkeyServiceMock
                .Setup(s => s.GetRegisteredHotkeys())
                .Returns(() => registered.ToArray());

            var result = _service.RegisterHotKey("Ctrl+Alt+K", () => { });

            Assert.True(result);
            Assert.Contains("Ctrl + Alt + K", _service.GetRegisteredHotKeys());

            // 取消注册后列表应被清理，避免残留
            var unregisterResult = _service.UnregisterHotKey("Ctrl+Alt+K");

            Assert.True(unregisterResult);
            Assert.DoesNotContain("Ctrl + Alt + K", _service.GetRegisteredHotKeys());
        }

        [Fact]
        public void RegisterHotKey_ShouldReturnFalse_WhenInputInvalid()
        {
            // 空字符串或空回调都不应注册，并返回 false
            var nullKeyResult = _service.RegisterHotKey(string.Empty, () => { });
            var nullCallbackResult = _service.RegisterHotKey("Ctrl+Q", null!);

            Assert.False(nullKeyResult);
            Assert.False(nullCallbackResult);
        }

        [Fact]
        public void IsHotKeyAvailable_ShouldReflectRegistrationState()
        {
            _globalHotkeyServiceMock
                .Setup(s => s.IsAvailable(It.IsAny<HotkeyDescriptor>()))
                .Returns(true);

            var available = _service.IsHotKeyAvailable("Shift+F2");

            Assert.True(available);
            _globalHotkeyServiceMock.Verify(
                s => s.IsAvailable(new HotkeyDescriptor(VirtualKey.F2, KeyModifiers.Shift)),
                Times.Once);
        }

        [Fact]
        public void HotKeyPressed_ShouldForwardFromGlobalHotkeyService()
        {
            string? received = null;
            _service.HotKeyPressed += (_, s) => received = s;

            _globalHotkeyServiceMock.Raise(
                s => s.HotkeyPressed += null,
                new HotkeyEventArgs(new HotkeyDescriptor(VirtualKey.V, KeyModifiers.Win)));

            Assert.Equal("Win + V", received);
        }

        [Fact]
        public void IsHotKeyAvailable_InvalidString_ShouldReturnFalse()
        {
            // 解析失败时捕获异常并返回 false，避免向底层注册器传递非法字符串
            var available = _service.IsHotKeyAvailable("Ctrl+");

            Assert.False(available);
            _globalHotkeyServiceMock.Verify(s => s.IsAvailable(It.IsAny<HotkeyDescriptor>()), Times.Never);
        }
    }
}
