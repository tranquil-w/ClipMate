using Avalonia.Media.Imaging;
using ClipMate.Core.Models;
using ClipMate.Core.Search;
using ClipMate.Infrastructure;
using ClipMate.Platform.Abstractions.Clipboard;
using Serilog;

namespace ClipMate.Presentation.Clipboard;

/// <summary>
/// 图像剪贴板内容实现（Avalonia）
/// </summary>
public class ImageClipboard : IClipboardContent
{
    public ClipboardItem Value { get; }

    public Bitmap ImageContent { get; }

    public string Summary => $"{Value.ContentType} {Value.Id}";

    public bool IsFavorite { get => Value.IsFavorite; set => Value.IsFavorite = value; }

    private static readonly ILogger _logger = Log.ForContext<ImageClipboard>();
    private readonly string _contentTypeLower;
    private readonly IClipboardWriter _clipboardWriter;

    public ImageClipboard(ClipboardItem item, Bitmap previewImage, IClipboardWriter clipboardWriter)
    {
        Value = item;
        ImageContent = previewImage;
        _contentTypeLower = item.ContentType.ToLowerInvariant();
        _clipboardWriter = clipboardWriter;
    }

    public async Task CopyAsync()
    {
        var success = await _clipboardWriter.TrySetAsync(
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
/// 图像剪贴板内容工厂（Avalonia）
/// </summary>
public class ImageClipboardFactory
{
    private readonly IClipboardWriter _clipboardWriter;

    public ImageClipboardFactory(IClipboardWriter clipboardWriter, int previewMaxPixelHeight = 240)
    {
        _clipboardWriter = clipboardWriter;
        _ = Math.Max(48, previewMaxPixelHeight);
    }

    public IClipboardContent Create(ClipboardItem item)
    {
        var preview = AvaloniaBitmapCodec.DecodeBitmap(item.Content);
        return new ImageClipboard(item, preview, _clipboardWriter);
    }

    public IClipboardContent Create(object content)
    {
        if (content is not Bitmap image)
        {
            throw new NotSupportedException();
        }

        var imageBytes = AvaloniaBitmapCodec.EncodePngBytes(image);

        ClipboardItem item = new()
        {
            ContentType = Constants.Image,
            Content = imageBytes,
            CreatedAt = DateTime.Now
        };

        var preview = AvaloniaBitmapCodec.DecodeBitmap(imageBytes);
        return new ImageClipboard(item, preview, _clipboardWriter);
    }

    private sealed class AvaloniaBitmapCodec
    {
        public static Bitmap DecodeBitmap(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            var bitmap = new Bitmap(stream);
            return bitmap;
        }

        public static byte[] EncodePngBytes(Bitmap bitmap)
        {
            using var stream = new MemoryStream();
            bitmap.Save(stream);
            return stream.ToArray();
        }
    }
}
