using ClipMate.Platform.Abstractions.Clipboard;
using ClipMate.Platform.Windows.Interop;
using Serilog;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SystemClipboard = System.Windows.Clipboard;

namespace ClipMate.Platform.Windows.Clipboard;

public sealed class WindowsClipboardChangeSource(ILogger logger) : IClipboardChangeSource
{
    private static readonly IntPtr _messageOnlyWindow = new(-3); // HWND_MESSAGE
    private readonly ILogger _logger = logger;
    private readonly TimeSpan _interval = TimeSpan.FromMilliseconds(50);
    private DateTime _lastClipboardChangeTime = DateTime.MinValue;
    private HwndSource? _hwndSource;
    private bool _isMonitoring;
    private bool _firstNotificationSkipped;
    private long _imageEncodeSequence;

    public event EventHandler<ClipboardPayloadChangedEventArgs>? ClipboardChanged;

    public void Start()
    {
        if (_isMonitoring)
        {
            _logger.Debug("剪贴板监听已在运行中");
            return;
        }

        try
        {
            _firstNotificationSkipped = false;
            _hwndSource = CreateClipboardHwndSource();
            _hwndSource.AddHook(WndProc);
            ClipboardNotification.RegisterClipboardViewer(_hwndSource.Handle);

            _isMonitoring = true;
            _logger.Information("剪贴板监听已启动");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "启动剪贴板监听失败");
            throw;
        }
    }

    public void Stop()
    {
        if (!_isMonitoring)
        {
            _logger.Debug("剪贴板监听未在运行");
            return;
        }

        try
        {
            if (_hwndSource != null)
            {
                ClipboardNotification.UnregisterClipboardViewer(_hwndSource.Handle);
                _hwndSource.RemoveHook(WndProc);
                _hwndSource = null;
            }

            _isMonitoring = false;
            _logger.Information("剪贴板监听已停止");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "停止剪贴板监听失败");
            throw;
        }
    }

    private HwndSource CreateClipboardHwndSource()
    {
        if (Application.Current?.MainWindow is Window mainWindow)
        {
            var helper = new WindowInteropHelper(mainWindow);
            var handle = helper.Handle != IntPtr.Zero ? helper.Handle : helper.EnsureHandle();

            if (handle != IntPtr.Zero)
            {
                var source = HwndSource.FromHwnd(handle);
                if (source != null)
                    return source;

                _logger.Warning("主窗口句柄可用但创建 HwndSource 失败，转为创建隐藏监听窗口");
            }
            else
            {
                _logger.Warning("主窗口句柄不可用，转为创建隐藏监听窗口");
            }
        }

        var parameters = new HwndSourceParameters("ClipMateClipboardListener")
        {
            Width = 0,
            Height = 0,
            PositionX = 0,
            PositionY = 0,
            ParentWindow = _messageOnlyWindow
        };
        return new HwndSource(parameters);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == ClipboardNotification.WM_DRAWCLIPBOARD)
        {
            if (!_firstNotificationSkipped)
                _firstNotificationSkipped = true;
            else
                OnClipboardChanged();
        }

        return IntPtr.Zero;
    }

    private void OnClipboardChanged()
    {
        var now = DateTime.Now;
        if (now - _lastClipboardChangeTime < _interval)
        {
            _logger.Debug("剪贴板变化事件间隔过短，跳过处理");
            return;
        }

        _lastClipboardChangeTime = now;

        try
        {
            if (SystemClipboard.ContainsFileDropList())
            {
                StringCollection? fileDropList = SystemClipboard.GetFileDropList();
                if (fileDropList != null && fileDropList.Count > 0)
                {
                    var files = fileDropList.Cast<string>().Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
                    if (files.Length == 0)
                        return;

                    ClipboardChanged?.Invoke(
                        this,
                        new ClipboardPayloadChangedEventArgs(
                            new ClipboardPayload(ClipboardPayloadType.FileDropList, FilePaths: files)));
                }
                else
                {
                    _logger.Warning("剪贴板包含文件列表但获取为空");
                }

                return;
            }

            if (SystemClipboard.ContainsImage())
            {
                var image = SystemClipboard.GetImage();
                if (image != null)
                {
                    TryRaiseImagePayloadAsync(image);
                }
                else
                {
                    _logger.Warning("剪贴板包含图片但获取失败");
                }

                return;
            }

            if (SystemClipboard.ContainsText())
            {
                var content = SystemClipboard.GetText();
                if (!string.IsNullOrEmpty(content))
                {
                    ClipboardChanged?.Invoke(
                        this,
                        new ClipboardPayloadChangedEventArgs(
                            new ClipboardPayload(ClipboardPayloadType.Text, Text: content)));
                }
                else
                {
                    _logger.Debug("剪贴板包含文本但内容为空");
                }

                return;
            }

            _logger.Debug("剪贴板内容不是文件、图片或文本，跳过处理");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "获取剪贴板内容失败");
        }
    }

    private static byte[] EncodePng(BitmapSource image)
    {
        using var memoryStream = new MemoryStream();
        BitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        encoder.Save(memoryStream);
        return memoryStream.ToArray();
    }

    private void TryRaiseImagePayloadAsync(BitmapSource image)
    {
        try
        {
            if (image.CanFreeze)
            {
                image.Freeze();
            }

            var sequence = Interlocked.Increment(ref _imageEncodeSequence);
            var dispatcher = _hwndSource?.Dispatcher ?? Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            _ = Task.Run(() =>
            {
                try
                {
                    var pngBytes = EncodePng(image);

                    dispatcher.InvokeAsync(() =>
                    {
                        if (sequence != Interlocked.Read(ref _imageEncodeSequence))
                        {
                            return;
                        }

                        ClipboardChanged?.Invoke(
                            this,
                            new ClipboardPayloadChangedEventArgs(
                                new ClipboardPayload(ClipboardPayloadType.ImagePng, ImagePngBytes: pngBytes)));
                    }, DispatcherPriority.Background);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "编码剪贴板图片失败");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "准备剪贴板图片负载失败");
        }
    }
}
