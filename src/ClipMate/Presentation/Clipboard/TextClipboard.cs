using ClipMate.Core.Models;
using ClipMate.Core.Search;
using ClipMate.Infrastructure;
using ClipMate.Platform.Abstractions.Clipboard;
using Serilog;
using System;
using System.Text;

namespace ClipMate.Presentation.Clipboard;

/// <summary>
/// 文本剪贴板内容实现，处理文本类型的剪贴板项
/// </summary>
public class TextClipboard : IClipboardContent
{
    public ClipboardItem Value { get; }
    public string TextContent { get; }
    public string Summary => TextContent.Length > 20 ? string.Concat(TextContent.AsSpan(0, 20), "...") : TextContent;
    public bool IsFavorite { get => Value.IsFavorite; set => Value.IsFavorite = value; }
    private static readonly ILogger _logger = Log.ForContext<TextClipboard>();
    private readonly string _searchableText;
    private readonly bool _isSearchTextTruncated;
    private readonly IClipboardWriter _clipboardWriter;

    public TextClipboard(ClipboardItem item, string text, IClipboardWriter clipboardWriter)
    {
        Value = item;
        TextContent = text;
        _clipboardWriter = clipboardWriter;
        (_searchableText, _isSearchTextTruncated) = BuildSearchableText(text);
    }

    public async Task CopyAsync()
    {
        bool success = await _clipboardWriter.TrySetAsync(
            new ClipboardPayload(ClipboardPayloadType.Text, Text: TextContent));
        if (!success)
        {
            _logger.Warning("无法将文本复制到剪贴板: {summary}", Summary);
        }
    }

    public bool IsVisible(SearchQuerySnapshot query)
    {
        if (!query.HasQuery)
            return true;

        if (_searchableText.Contains(query.LowerInvariant, StringComparison.Ordinal))
            return true;

        return _isSearchTextTruncated && TextContent.Contains(query.Normalized, StringComparison.OrdinalIgnoreCase);
    }

    private static (string searchableText, bool isTruncated) BuildSearchableText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return (string.Empty, false);

        if (text.Length <= SearchConstants.MaxSearchTextLength)
            return (text.ToLowerInvariant(), false);

        return (text[..SearchConstants.MaxSearchTextLength].ToLowerInvariant(), true);
    }
}

/// <summary>
/// 文本剪贴板内容工厂，创建 TextClipboard 实例
/// </summary>
public class TextClipboardFactory
{
    private readonly IClipboardWriter _clipboardWriter;

    public TextClipboardFactory(IClipboardWriter clipboardWriter)
    {
        _clipboardWriter = clipboardWriter;
    }

    public IClipboardContent Create(ClipboardItem item)
    {
        var text = Encoding.UTF8.GetString(item.Content);
        return new TextClipboard(item, text, _clipboardWriter);
    }

    public IClipboardContent Create(object content)
    {
        if (content is not string text)
            throw new NotSupportedException();

        ClipboardItem item = new()
        {
            ContentType = Constants.Text,
            Content = Encoding.UTF8.GetBytes(text),
            CreatedAt = DateTime.Now
        };
        return Create(item);
    }
}
