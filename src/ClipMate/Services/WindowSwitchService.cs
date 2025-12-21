using ClipMate.Service.Interfaces;
using ClipMate.Platform.Windows.Interop;
using Serilog;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;

namespace ClipMate.Services
{
    public class WindowSwitchService : IWindowSwitchService
    {
        private static IntPtr _clipMateWindow;
        private static IntPtr _pastingWindow;
        private static IntPtr _taskbarHandle;
        private readonly Timer _timer;
        private readonly ILogger _logger;

        public WindowSwitchService(ILogger logger)
        {
            _logger = logger;
            _logger.Information("初始化窗口切换服务");

            _taskbarHandle = WindowSwitchNative.FindWindow("Shell_TrayWnd", null);
            _logger.Debug("任务栏句柄：{TaskbarHandle}", _taskbarHandle);

            _timer = new(UpdatePastingWindow, this, 1000, 200);

            Application.Current.MainWindow.Loaded += (sender, e) =>
            {
                _clipMateWindow = new WindowInteropHelper((Window)sender).Handle;
                _logger.Debug("ClipMate 窗口句柄：{ClipMateHandle}", _clipMateWindow);
            };

            Application.Current.Exit += (sender, e) =>
            {
                _timer.Dispose();
                _logger.Debug("窗口切换服务已停止");
            };
        }

        private static void UpdatePastingWindow(object? state)
        {
            var service = state as WindowSwitchService;

            // 记录除ClipMate窗口和任务栏之外的窗口
            var window = WindowSwitchNative.GetForegroundWindow();
            if (window != _clipMateWindow && window != _taskbarHandle && window != IntPtr.Zero)
            {
                if (_pastingWindow != window)
                {
                    _pastingWindow = window;
                    service?._logger.Debug("记录粘贴目标窗口：{WindowHandle}", window);
                }
            }
        }

        public void SwitchToPastingWindow()
        {
            if (_pastingWindow == IntPtr.Zero)
            {
                _logger.Debug("粘贴目标窗口为空，无法切换");
                return;
            }

            try
            {
                // 切换到记录的窗口
                var success = WindowSwitchNative.SetForegroundWindow(_pastingWindow);
                if (success)
                {
                    _logger.Information("成功切换到粘贴目标窗口：{WindowHandle}", _pastingWindow);
                }
                else
                {
                    _logger.Warning("切换到粘贴目标窗口失败：{WindowHandle}", _pastingWindow);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "切换窗口时发生异常，目标窗口：{WindowHandle}", _pastingWindow);
                throw;
            }
        }

        public nint GetPastingWindow()
        {
            return _pastingWindow;
        }
    }
}
