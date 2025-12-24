using Avalonia.Media.Imaging;
using ClipMate.Core.Models;
using ClipMate.Infrastructure;
using ClipMate.Platform.Abstractions.Clipboard;
using ClipMate.Presentation.Clipboard;
using ClipMate.Service.Clipboard;
using ClipMate.Service.Interfaces;
using ClipMate.Services;
using Serilog;
using System.Collections.Specialized;

namespace ClipMate.Avalonia.Services;

public sealed class AvaloniaClipboardService(
    ISettingsService settingsService,
    IClipboardWriter clipboardWriter,
    IClipboardPasteUseCase clipboardPasteUseCase,
    ILogger logger) : IClipboardService
{
    private readonly ILogger _logger = logger;
    private readonly IClipboardPasteUseCase _clipboardPasteUseCase = clipboardPasteUseCase;
    private readonly ImageClipboardFactory _imageClipboardFactory = new(clipboardWriter, CalculatePreviewHeight(settingsService));
    private readonly TextClipboardFactory _textClipboardFactory = new(clipboardWriter);
    private readonly FileDropListClipboardFactory _fileDropListClipboardFactory = new(clipboardWriter);

    private static int CalculatePreviewHeight(ISettingsService settingsService)
    {
        var height = settingsService.GetClipboardItemMaxHeight();
        if (height <= 0)
        {
            height = 100;
        }

        return Math.Clamp(height * 2, 64, 512);
    }

    public IClipboardContent Create(ClipboardItem item)
    {
        try
        {
            var content = item.ContentType switch
            {
                Constants.Text => _textClipboardFactory.Create(item),
                Constants.Image => _imageClipboardFactory.Create(item),
                Constants.FileDropList => _fileDropListClipboardFactory.Create(item),
                _ => throw new NotSupportedException($"不支持的内容类型：{item.ContentType}")
            };

            return content;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "创建剪贴板内容失败，类型：{ContentType}，ID：{Id}", item.ContentType, item.Id);
            throw;
        }
    }

    public IClipboardContent Create(object content)
    {
        try
        {
            IClipboardContent clipboardContent;
            string contentType;

            if (content is string text)
            {
                clipboardContent = _textClipboardFactory.Create(text);
                contentType = "文本";
            }
            else if (content is Bitmap image)
            {
                clipboardContent = _imageClipboardFactory.Create(image);
                contentType = "图片";
            }
            else if (content is IEnumerable<string> fileList)
            {
                var collection = new StringCollection();
                foreach (var path in fileList)
                {
                    collection.Add(path);
                }

                clipboardContent = _fileDropListClipboardFactory.Create(collection);
                contentType = "文件";
            }
            else
            {
                throw new NotSupportedException($"不支持的内容类型：{content.GetType().Name}");
            }

            _logger.Debug("从对象创建剪贴板内容，类型：{ContentType}", contentType);
            return clipboardContent;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "从对象创建剪贴板内容失败，对象类型：{ObjectType}", content.GetType().Name);
            throw;
        }
    }

    public async Task PasteAsync(IClipboardContent item)
    {
        try
        {
            _logger.Information("开始执行粘贴操作");
            await _clipboardPasteUseCase.PasteAsync(item.Value);
            _logger.Information("粘贴操作完成");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "粘贴操作失败");
            throw;
        }
    }
}
