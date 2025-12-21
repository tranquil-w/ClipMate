using ClipMate.Platform.Abstractions.Clipboard;
using Serilog;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using SystemClipboard = System.Windows.Clipboard;

namespace ClipMate.Platform.Windows.Clipboard;

public sealed class WindowsClipboardWriter(ILogger logger) : IClipboardWriter
{
    private const int MaxRetryCount = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);

    private readonly ILogger _logger = logger;

    public Task<bool> TrySetAsync(ClipboardPayload payload, CancellationToken cancellationToken = default)
    {
        return ExecuteWithRetryAsync(
            operationName: $"剪贴板写入({payload.Type})",
            operation: () => Set(payload),
            cancellationToken: cancellationToken);
    }

    private void Set(ClipboardPayload payload)
    {
        switch (payload.Type)
        {
            case ClipboardPayloadType.Text:
                SystemClipboard.SetText(payload.Text ?? string.Empty);
                return;

            case ClipboardPayloadType.ImagePng:
                var imageBytes = payload.ImagePngBytes ?? Array.Empty<byte>();
                if (imageBytes.Length == 0)
                {
                    SystemClipboard.SetText(string.Empty);
                    return;
                }

                var bitmap = DecodePng(imageBytes);
                SystemClipboard.SetImage(bitmap);
                return;

            case ClipboardPayloadType.FileDropList:
                var paths = payload.FilePaths?.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray() ?? Array.Empty<string>();
                var collection = new StringCollection();
                collection.AddRange(paths);
                SystemClipboard.SetFileDropList(collection);
                return;

            default:
                throw new NotSupportedException($"不支持的剪贴板负载类型：{payload.Type}");
        }
    }

    private async Task<bool> ExecuteWithRetryAsync(
        string operationName,
        Action operation,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxRetryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await InvokeOnDispatcherAsync(operation, cancellationToken);
                return true;
            }
            catch (Exception ex) when (attempt < MaxRetryCount)
            {
                _logger.Warning(ex, "{Operation} 失败(尝试 {Attempt}/{Max})", operationName, attempt, MaxRetryCount);
                await Task.Delay(TimeSpan.FromMilliseconds(RetryDelay.TotalMilliseconds * attempt), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "{Operation} 最终失败", operationName);
                return false;
            }
        }

        return false;
    }

    private static Task InvokeOnDispatcherAsync(Action action, CancellationToken cancellationToken)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(action).Task.WaitAsync(cancellationToken);
    }

    private static BitmapSource DecodePng(byte[] pngBytes)
    {
        using var stream = new MemoryStream(pngBytes, writable: false);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}

