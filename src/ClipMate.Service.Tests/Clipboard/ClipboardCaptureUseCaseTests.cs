using ClipMate.Core.Models;
using ClipMate.Platform.Abstractions.Clipboard;
using ClipMate.Service.Clipboard;
using Moq;
using System.Text;
using System.Text.Json;

namespace ClipMate.Service.Tests.Clipboard;

public class ClipboardCaptureUseCaseTests
{
    [Fact]
    public async Task CaptureAsync_WithText_ShouldInsertUtf8TextItem()
    {
        var repository = new Mock<IClipboardItemRepository>();
        repository.Setup(r => r.InsertAsync(It.IsAny<ClipboardItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(123);

        var useCase = new ClipboardCaptureUseCase(repository.Object);
        var payload = new ClipboardPayload(ClipboardPayloadType.Text, Text: "你好");

        var result = await useCase.CaptureAsync(payload);

        Assert.Equal(123, result.Id);
        Assert.NotNull(result.Item);
        Assert.Equal(123, result.Item!.Id);
        Assert.Equal(ClipboardContentTypes.Text, result.Item.ContentType);
        Assert.Equal("你好", Encoding.UTF8.GetString(result.Item.Content));

        repository.Verify(r => r.InsertAsync(
            It.Is<ClipboardItem>(item => item.ContentType == ClipboardContentTypes.Text),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CaptureAsync_WithFileDropList_ShouldSerializeToJsonUtf8()
    {
        var repository = new Mock<IClipboardItemRepository>();
        repository.Setup(r => r.InsertAsync(It.IsAny<ClipboardItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        var useCase = new ClipboardCaptureUseCase(repository.Object);
        var payload = new ClipboardPayload(ClipboardPayloadType.FileDropList, FilePaths: new[] { @"C:\a.txt", @"C:\b.doc" });

        var result = await useCase.CaptureAsync(payload);

        Assert.Equal(7, result.Id);
        Assert.NotNull(result.Item);
        Assert.Equal(ClipboardContentTypes.FileDropList, result.Item!.ContentType);

        var json = Encoding.UTF8.GetString(result.Item.Content);
        var paths = JsonSerializer.Deserialize<string[]>(json);
        Assert.Equal(new[] { @"C:\a.txt", @"C:\b.doc" }, paths);
    }

    [Fact]
    public async Task CaptureAsync_WithDuplicateRepositoryResult_ShouldReturnDuplicate()
    {
        var repository = new Mock<IClipboardItemRepository>();
        repository.Setup(r => r.InsertAsync(It.IsAny<ClipboardItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(-1);

        var useCase = new ClipboardCaptureUseCase(repository.Object);
        var payload = new ClipboardPayload(ClipboardPayloadType.Text, Text: "dup");

        var result = await useCase.CaptureAsync(payload);

        Assert.Equal(-1, result.Id);
        Assert.Null(result.Item);
    }
}

