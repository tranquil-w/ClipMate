using ClipMate.Core.Models;
using ClipMate.Platform.Abstractions.Clipboard;
using System.Text;
using System.Text.Json;

namespace ClipMate.Service.Clipboard;

public sealed class ClipboardCaptureUseCase(IClipboardItemRepository repository) : IClipboardCaptureUseCase
{
    private readonly IClipboardItemRepository _repository = repository;

    public async Task<ClipboardCaptureResult> CaptureAsync(ClipboardPayload payload, CancellationToken cancellationToken = default)
    {
        var item = CreateClipboardItem(payload);
        if (item == null)
            return ClipboardCaptureResult.Duplicate;

        var id = await _repository.InsertAsync(item, cancellationToken);
        if (id < 0)
            return ClipboardCaptureResult.Duplicate;

        item.Id = id;
        return new ClipboardCaptureResult(id, item);
    }

    private static ClipboardItem? CreateClipboardItem(ClipboardPayload payload)
    {
        switch (payload.Type)
        {
            case ClipboardPayloadType.Text:
                if (string.IsNullOrEmpty(payload.Text))
                    return null;

                return new ClipboardItem
                {
                    ContentType = ClipboardContentTypes.Text,
                    Content = Encoding.UTF8.GetBytes(payload.Text),
                    CreatedAt = DateTime.Now
                };

            case ClipboardPayloadType.ImagePng:
                if (payload.ImagePngBytes == null || payload.ImagePngBytes.Length == 0)
                    return null;

                return new ClipboardItem
                {
                    ContentType = ClipboardContentTypes.Image,
                    Content = payload.ImagePngBytes,
                    CreatedAt = DateTime.Now
                };

            case ClipboardPayloadType.FileDropList:
                var files = payload.FilePaths?.Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();
                if (files == null || files.Length == 0)
                    return null;

                return new ClipboardItem
                {
                    ContentType = ClipboardContentTypes.FileDropList,
                    Content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(files)),
                    CreatedAt = DateTime.Now
                };

            default:
                throw new NotSupportedException($"不支持的剪贴板负载类型：{payload.Type}");
        }
    }
}

