using ClipMate.Core.Models;
using ClipMate.Core.Search;
using ClipMate.Infrastructure;
using ClipMate.Platform.Abstractions.Clipboard;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace ClipMate.Presentation.Clipboard;

/// <summary>
/// 文件列表剪贴板内容实现，处理文件拖放类型的剪贴板项
/// </summary>
public class FileDropListClipboard : IClipboardContent
{
    public ClipboardItem Value { get; }
    public StringCollection FilePathList { get; }
    public string Summary => BuildSummary();
    public bool IsFavorite { get => Value.IsFavorite; set => Value.IsFavorite = value; }
    private static readonly ILogger _logger = Log.ForContext<FileDropListClipboard>();
    private readonly IClipboardWriter _clipboardWriter;
    private readonly List<string> _fileNames;
    private readonly List<string> _extensions;
    private readonly List<string> _fullPaths;

    public FileDropListClipboard(ClipboardItem item, StringCollection filePathList, IClipboardWriter clipboardWriter)
    {
        Value = item;
        FilePathList = filePathList;
        _clipboardWriter = clipboardWriter;
        (_fileNames, _extensions, _fullPaths) = BuildSearchIndex(filePathList);
    }

    public async Task CopyAsync()
    {
        var (existingPaths, missingPaths) = FilterExistingPaths();
        var targetList = missingPaths.Count > 0 ? existingPaths : FilePathList;

        if (missingPaths.Count > 0)
        {
            if (existingPaths.Count == 0)
            {
                _logger.Error("所有文件路径均不存在，无法执行粘贴: {Paths}", missingPaths);
                return;
            }

            _logger.Warning("部分文件路径不存在，已跳过: {Paths}", missingPaths);
        }

        var filePaths = targetList.Cast<string>().Where(path => !string.IsNullOrWhiteSpace(path)).ToArray();
        bool success = await _clipboardWriter.TrySetAsync(
            new ClipboardPayload(ClipboardPayloadType.FileDropList, FilePaths: filePaths));
        if (!success)
        {
            _logger.Warning("无法将文件列表复制到剪贴板: {summary}", Summary);
        }
        else
        {
            _logger.Information("已复制文件列表到剪贴板，数量: {Count}", targetList.Count);
        }
    }

    public bool IsVisible(SearchQuerySnapshot query)
    {
        if (!query.HasQuery)
            return true;

        foreach (var fileName in _fileNames)
        {
            if (fileName.Contains(query.LowerInvariant, StringComparison.Ordinal))
                return true;
        }

        foreach (var extension in _extensions)
        {
            if (extension.Equals(query.LowerInvariantNoDot, StringComparison.Ordinal))
                return true;
        }

        foreach (var path in _fullPaths)
        {
            if (path.Contains(query.LowerInvariant, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private string BuildSummary()
    {
        var firstPath = FilePathList.Cast<string?>().FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));
        if (string.IsNullOrEmpty(firstPath))
            return "📄 文件";

        bool isDirectory = Directory.Exists(firstPath);
        string name = GetDisplayName(firstPath, isDirectory);
        string prefix = isDirectory ? "📁 " : "📄 ";
        string countUnit = isDirectory ? "个项目" : "个文件";

        if (FilePathList.Count > 1)
            return $"{prefix}{name} (+{FilePathList.Count - 1} {countUnit})";

        return $"{prefix}{name}";
    }

    private string GetDisplayName(string path, bool isDirectory)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "文件";

        string fileName = isDirectory
            ? Path.GetFileName(Path.TrimEndingDirectorySeparator(path))
            : Path.GetFileName(path);

        if (string.IsNullOrEmpty(fileName))
            fileName = path;

        return fileName.Length > DisplayConstants.MaxFileNameLength
            ? string.Concat(fileName.AsSpan(0, DisplayConstants.MaxFileNameLength), "...")
            : fileName;
    }

    private (StringCollection existingPaths, List<string> missingPaths) FilterExistingPaths()
    {
        StringCollection existing = new();
        List<string> missing = [];

        foreach (string? path in FilePathList)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            if (File.Exists(path) || Directory.Exists(path))
            {
                existing.Add(path);
            }
            else
            {
                missing.Add(path);
            }
        }

        return (existing, missing);
    }

    private static (List<string> fileNames, List<string> extensions, List<string> fullPaths) BuildSearchIndex(StringCollection filePathList)
    {
        List<string> fileNames = new();
        List<string> extensions = new();
        List<string> fullPaths = new();

        foreach (string? rawPath in filePathList)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
                continue;

            var normalizedPath = rawPath.Trim();
            fullPaths.Add(normalizedPath.ToLowerInvariant());

            var fileName = Directory.Exists(normalizedPath)
                ? Path.GetFileName(Path.TrimEndingDirectorySeparator(normalizedPath))
                : Path.GetFileName(normalizedPath);

            if (!string.IsNullOrEmpty(fileName))
            {
                fileNames.Add(fileName.ToLowerInvariant());
            }

            var extension = Path.GetExtension(normalizedPath);
            if (!string.IsNullOrEmpty(extension))
            {
                extensions.Add(extension.TrimStart('.').ToLowerInvariant());
            }
        }

        return (fileNames, extensions, fullPaths);
    }
}

/// <summary>
/// 文件列表剪贴板内容工厂，创建 FileDropListClipboard 实例
/// </summary>
public class FileDropListClipboardFactory
{
    private static readonly ILogger _logger = Log.ForContext<FileDropListClipboardFactory>();
    private readonly IClipboardWriter _clipboardWriter;

    public FileDropListClipboardFactory(IClipboardWriter clipboardWriter)
    {
        _clipboardWriter = clipboardWriter;
    }

    public IClipboardContent Create(ClipboardItem item)
    {
        try
        {
            string json = Encoding.UTF8.GetString(item.Content);
            string[] filePaths = JsonSerializer.Deserialize<string[]>(json) ?? [];

            StringCollection collection = new();
            foreach (var path in filePaths.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                collection.Add(path);
            }

            return new FileDropListClipboard(item, collection, _clipboardWriter);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "创建文件剪贴板内容失败，ID：{Id}", item.Id);
            throw;
        }
    }

    public IClipboardContent Create(object content)
    {
        if (content is not StringCollection filePathList)
            throw new NotSupportedException();

        try
        {
            string[] paths = filePathList.Cast<string?>()
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Cast<string>()
                .ToArray();

            ClipboardItem item = new()
            {
                ContentType = Constants.FileDropList,
                Content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(paths)),
                CreatedAt = DateTime.Now
            };

            return Create(item);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "序列化文件列表失败");
            throw;
        }
    }
}
