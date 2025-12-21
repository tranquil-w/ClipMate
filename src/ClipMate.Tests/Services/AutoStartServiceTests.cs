using System;
using ClipMate.Infrastructure;
using ClipMate.Tests.TestHelpers;
using ClipMate.Platform.Windows.Startup;
using Microsoft.Win32;
using Moq;
using Serilog;
using System.IO;
using System.Reflection;
using Xunit;

namespace ClipMate.Tests.Services
{
    /// <summary>
    /// WindowsAutoStartService 的注册表交互单元测试。
    /// 通过备份/还原 Run 项中的 ClipMate 值来隔离测试，避免对系统开机自启配置留下持久修改。
    /// </summary>
    public class AutoStartServiceTests : TestBase, IDisposable
    {
        private readonly Mock<ILogger> _loggerMock = new();
        private readonly WindowsAutoStartService _service;
        private readonly RegistryRunSandbox _sandbox;

        public AutoStartServiceTests()
        {
            _sandbox = new RegistryRunSandbox();
            _service = new WindowsAutoStartService(_loggerMock.Object);
        }

        public override void Dispose()
        {
            _sandbox.Dispose();
            base.Dispose();
        }

        /// <summary>
        /// 当注册表值与当前可执行文件路径一致时，IsAutoStartEnabled 应返回 true。
        /// </summary>
        [Fact]
        public void IsAutoStartEnabled_ShouldReturnTrue_WhenRegistryMatchesExecutable()
        {
            _sandbox.SetCustomValue($"\"{GetExpectedExecutablePath()}\"");

            var enabled = InvokeIsRegistryAutoStartEnabled();

            Assert.True(enabled, $"Expected IsRegistryAutoStartEnabled to return true. Registry value: {_sandbox.GetValue()}");
            Assert.Equal($"\"{GetExpectedExecutablePath()}\"", _sandbox.GetValue());
        }

        /// <summary>
        /// 未设置自启值时，IsAutoStartEnabled 应返回 false。
        /// </summary>
        [Fact]
        public void IsAutoStartEnabled_ShouldReturnFalse_WhenValueMissing()
        {
            var enabled = InvokeIsRegistryAutoStartEnabled();

            Assert.False(enabled);
        }

        /// <summary>
        /// 注册表中的路径与当前进程不一致时，应返回 false，防止误判为已启用。
        /// </summary>
        [Fact]
        public void IsAutoStartEnabled_ShouldReturnFalse_WhenPathMismatch()
        {
            _sandbox.SetCustomValue("\"C:\\\\FakeApp.exe\"");

            var enabled = InvokeIsRegistryAutoStartEnabled();

            Assert.False(enabled);
        }

        /// <summary>
        /// SetAutoStart(true) 应写入带引号的可执行路径，并记录信息日志。
        /// </summary>
        [Fact]
        public void SetAutoStart_ShouldWriteExecutablePath_WhenEnabled()
        {
            InvokeSetAutoStartWithRegistry(true);

            Assert.Equal($"\"{GetExpectedExecutablePath()}\"", _sandbox.GetValue());
            _loggerMock.Verify(
                l => l.Information<string>(It.Is<string>(msg => msg.Contains("开机自启动")), It.IsAny<string>()),
                Times.AtLeastOnce);
        }

        /// <summary>
        /// SetAutoStart(false) 应删除注册表值并记录信息日志。
        /// </summary>
        [Fact]
        public void SetAutoStart_ShouldRemoveRegistryValue_WhenDisabled()
        {
            InvokeSetAutoStartWithRegistry(true);

            InvokeSetAutoStartWithRegistry(false);

            Assert.Null(_sandbox.GetValue());
            _loggerMock.Verify(
                l => l.Information(It.Is<string>(msg => msg.Contains("禁用开机自启动"))),
                Times.AtLeastOnce);
        }

        private static string GetExpectedExecutablePath()
        {
            var exePath = Environment.ProcessPath ?? typeof(WindowsAutoStartService).Assembly.Location;
            return Path.GetFullPath(exePath);
        }

        private bool InvokeIsRegistryAutoStartEnabled()
        {
            var method = typeof(WindowsAutoStartService).GetMethod("IsRegistryAutoStartEnabled", BindingFlags.NonPublic | BindingFlags.Instance)
                         ?? throw new InvalidOperationException("无法找到 IsRegistryAutoStartEnabled 方法用于测试。");
            return (bool)method.Invoke(_service, Array.Empty<object>())!;
        }

        private void InvokeSetAutoStartWithRegistry(bool enabled)
        {
            var method = typeof(WindowsAutoStartService).GetMethod("SetAutoStartWithRegistry", BindingFlags.NonPublic | BindingFlags.Instance)
                         ?? throw new InvalidOperationException("无法找到 SetAutoStartWithRegistry 方法用于测试。");
            method.Invoke(_service, new object[] { enabled });
        }
    }

    /// <summary>
    /// 针对 Run 注册表项的测试沙箱：备份原有值，测试结束后恢复，防止污染真实自启配置。
    /// </summary>
    internal sealed class RegistryRunSandbox : IDisposable
    {
        private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private readonly string ValueName = Constants.AppName;

        private readonly RegistryKey _runKey;
        private readonly object? _originalValue;
        private readonly RegistryValueKind _originalKind;
        private readonly bool _hasOriginal;

        public RegistryRunSandbox()
        {
            _runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath, true)
                ?? throw new InvalidOperationException("无法创建/打开 Run 注册表项用于测试。");

            var existing = _runKey.GetValue(ValueName);
            if (existing is not null)
            {
                _hasOriginal = true;
                _originalValue = existing;
                _originalKind = _runKey.GetValueKind(ValueName);
            }

            // 清理同名值，确保测试隔离
            _runKey.DeleteValue(ValueName, false);
        }

        public string? GetValue() => _runKey.GetValue(ValueName) as string;

        public void SetCustomValue(string value) => _runKey.SetValue(ValueName, value);

        public void Dispose()
        {
            try
            {
                // 清理本次测试写入
                _runKey.DeleteValue(ValueName, false);

                // 恢复原有值（如存在）
                if (_hasOriginal)
                {
                    _runKey.SetValue(ValueName, _originalValue!, _originalKind);
                }
            }
            finally
            {
                _runKey.Dispose();
            }
        }
    }
}
