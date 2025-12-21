using ClipMate.Core.Models;
using ClipMate.Core.Search;
using ClipMate.Infrastructure;
using ClipMate.Platform.Abstractions.Clipboard;
using Serilog;
using System;
using System.Windows.Media.Imaging;

namespace ClipMate.Presentation.Clipboard;

/// <summary>
/// 图像剪贴板内容实现，处理图像类型的剪贴板项
/// </summary>
public class ImageClipboard : IClipboardContent
{
    public ClipboardItem Value { get; }
    public BitmapSource ImageContent { get; }
    public string Summary => $"{Value.ContentType} {Value.Id}";
    public bool IsFavorite { get => Value.IsFavorite; set => Value.IsFavorite = value; }
    private static readonly ILogger _logger = Log.ForContext<ImageClipboard>();
    private readonly string _contentTypeLower;
    private readonly IClipboardWriter _clipboardWriter;

    public ImageClipboard(ClipboardItem item, BitmapSource previewImage, IClipboardWriter clipboardWriter)
    {
        Value = item;
        ImageContent = previewImage;
        _contentTypeLower = item.ContentType.ToLowerInvariant();
        _clipboardWriter = clipboardWriter;
    }

    public async Task CopyAsync()
    {
        bool success = await _clipboardWriter.TrySetAsync(
            new ClipboardPayload(ClipboardPayloadType.ImagePng, ImagePngBytes: Value.Content));
        if (!success)
        {
            _logger.Warning("无法将图像复制到剪贴板: {summary}", Summary);
        }
    }

    public bool IsVisible(SearchQuerySnapshot query)
    {
        return !query.HasQuery || _contentTypeLower.Contains(query.LowerInvariant, StringComparison.Ordinal);
    }
}

/// <summary>
/// 图像剪贴板内容工厂，创建 ImageClipboard 实例，支持预览图生成
/// </summary>
public class ImageClipboardFactory
{
    private readonly int _previewMaxPixelHeight;
    private readonly IClipboardWriter _clipboardWriter;

    public ImageClipboardFactory(IClipboardWriter clipboardWriter, int previewMaxPixelHeight = 240)
    {
        _clipboardWriter = clipboardWriter;
        _previewMaxPixelHeight = Math.Max(48, previewMaxPixelHeight);
    }

    public IClipboardContent Create(ClipboardItem item)
    {
        var preview = BitmapCodec.DecodeBitmapImage(item.Content, _previewMaxPixelHeight);
        return new ImageClipboard(item, preview, _clipboardWriter);
    }

    public IClipboardContent Create(object content)
    {
        if (content is not BitmapSource image)
            throw new NotSupportedException();

        var imageBytes = BitmapCodec.EncodePngBytes(image);

        ClipboardItem item = new()
        {
            ContentType = Constants.Image,
            Content = imageBytes,
            CreatedAt = DateTime.Now
        };

        var preview = BitmapCodec.DecodeBitmapImage(imageBytes, _previewMaxPixelHeight);
        return new ImageClipboard(item, preview, _clipboardWriter);
    }
}
