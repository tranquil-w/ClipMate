using ClipMate.Core.Models;
using ClipMate.Platform.Abstractions.Clipboard;
using ClipMate.Platform.Abstractions.Input;
using ClipMate.Platform.Abstractions.Window;
using Serilog;
using System;
using System.Text;
using System.Text.Json;

namespace ClipMate.Service.Clipboard;

public sealed class ClipboardPasteUseCase(
    IClipboardWriter clipboardWriter,
    IPasteTrigger pasteTrigger,
    IMainWindowController mainWindowController,
    IPasteTargetWindowService pasteTargetWindowService,
    ILogger logger) : IClipboardPasteUseCase
{
    private readonly IClipboardWriter _clipboardWriter = clipboardWriter;
    private readonly IPasteTrigger _pasteTrigger = pasteTrigger;
    private readonly IMainWindowController _mainWindowController = mainWindowController;
    private readonly IPasteTargetWindowService _pasteTargetWindowService = pasteTargetWindowService;
    private readonly ILogger _logger = logger;

    private static readonly TimeSpan HideTimeout = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan ForegroundRestoreTimeout = TimeSpan.FromMilliseconds(500);

    public async Task PasteAsync(ClipboardItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var payload = CreatePayload(item);
        var setSuccess = await _clipboardWriter.TrySetAsync(payload, cancellationToken);
        if (!setSuccess)
        {
            throw new InvalidOperationException("无法写入系统剪贴板");
        }

        try
        {
            await _mainWindowController.HideMainWindowAsync(cancellationToken).WaitAsync(HideTimeout, cancellationToken);
        }
        catch (TimeoutException ex)
        {
            _logger.Warning(ex, "等待主窗口隐藏超时（{TimeoutMs}ms），仍继续触发粘贴", HideTimeout.TotalMilliseconds);
        }

        var pasteTarget = _pasteTargetWindowService.PasteTargetWindowHandle;
        var waitStartedAt = DateTimeOffset.UtcNow;
        var (ready, currentForeground) = await _pasteTargetWindowService.WaitForReadyToPasteAsync(
            ForegroundRestoreTimeout,
            cancellationToken);
        if (!ready)
        {
            var waitedMs = (DateTimeOffset.UtcNow - waitStartedAt).TotalMilliseconds;
            _logger.Warning(
                "等待前台恢复超时（{TimeoutMs}ms），仍继续触发粘贴：WaitedMs={WaitedMs} CurrentForeground={CurrentForeground} PasteTarget={PasteTarget}",
                ForegroundRestoreTimeout.TotalMilliseconds,
                waitedMs,
                currentForeground,
                pasteTarget);
        }

        await _pasteTrigger.TriggerPasteAsync(cancellationToken);
    }

    private static ClipboardPayload CreatePayload(ClipboardItem item)
    {
        switch (item.ContentType)
        {
            case ClipboardContentTypes.Text:
                if (item.Content == null || item.Content.Length == 0)
                    return new ClipboardPayload(ClipboardPayloadType.Text, Text: string.Empty);

                return new ClipboardPayload(
                    ClipboardPayloadType.Text,
                    Text: Encoding.UTF8.GetString(item.Content));

            case ClipboardContentTypes.Image:
                return new ClipboardPayload(
                    ClipboardPayloadType.ImagePng,
                    ImagePngBytes: item.Content);

            case ClipboardContentTypes.FileDropList:
                if (item.Content == null || item.Content.Length == 0)
                    return new ClipboardPayload(ClipboardPayloadType.FileDropList, FilePaths: Array.Empty<string>());

                var json = Encoding.UTF8.GetString(item.Content);
                var paths = JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
                return new ClipboardPayload(ClipboardPayloadType.FileDropList, FilePaths: paths);

            default:
                throw new NotSupportedException($"不支持的内容类型：{item.ContentType}");
        }
    }
}
