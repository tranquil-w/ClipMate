using ClipMate.Core.Models;
using ClipMate.Platform.Abstractions.Clipboard;
using ClipMate.Platform.Abstractions.Input;
using ClipMate.Platform.Abstractions.Window;
using ClipMate.Service.Clipboard;
using Moq;
using Serilog;
using System;
using System.Text;
using System.Text.Json;

namespace ClipMate.Service.Tests.Clipboard;

public class ClipboardPasteUseCaseTests
{
    [Fact]
    public async Task PasteAsync_WithText_ShouldWriteTextPayload_ThenTriggerPaste()
    {
        var writer = new Mock<IClipboardWriter>();
        writer.Setup(w => w.TrySetAsync(It.IsAny<ClipboardPayload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var trigger = new Mock<IPasteTrigger>();
        trigger.Setup(t => t.TriggerPasteAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var window = new Mock<IMainWindowController>();
        window.Setup(w => w.HideMainWindowAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var pasteTarget = new Mock<IPasteTargetWindowService>();
        pasteTarget.Setup(p => p.WaitForReadyToPasteAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (nint)0));

        var logger = new Mock<ILogger>();

        var useCase = new ClipboardPasteUseCase(writer.Object, trigger.Object, window.Object, pasteTarget.Object, logger.Object);
        var item = new ClipboardItem { ContentType = ClipboardContentTypes.Text, Content = Encoding.UTF8.GetBytes("hello"), CreatedAt = DateTime.Now };

        await useCase.PasteAsync(item);

        writer.Verify(w => w.TrySetAsync(
            It.Is<ClipboardPayload>(p => p.Type == ClipboardPayloadType.Text && p.Text == "hello"),
            It.IsAny<CancellationToken>()), Times.Once);
        window.Verify(w => w.HideMainWindowAsync(It.IsAny<CancellationToken>()), Times.Once);
        pasteTarget.Verify(p => p.WaitForReadyToPasteAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        trigger.Verify(t => t.TriggerPasteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PasteAsync_WhenWriteClipboardFails_ShouldNotTriggerPaste()
    {
        var writer = new Mock<IClipboardWriter>();
        writer.Setup(w => w.TrySetAsync(It.IsAny<ClipboardPayload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var trigger = new Mock<IPasteTrigger>();
        var window = new Mock<IMainWindowController>();
        var pasteTarget = new Mock<IPasteTargetWindowService>();
        var logger = new Mock<ILogger>();

        var useCase = new ClipboardPasteUseCase(writer.Object, trigger.Object, window.Object, pasteTarget.Object, logger.Object);
        var item = new ClipboardItem { ContentType = ClipboardContentTypes.Text, Content = Encoding.UTF8.GetBytes("hello"), CreatedAt = DateTime.Now };

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.PasteAsync(item));

        window.Verify(w => w.HideMainWindowAsync(It.IsAny<CancellationToken>()), Times.Never);
        pasteTarget.Verify(p => p.WaitForReadyToPasteAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
        trigger.Verify(t => t.TriggerPasteAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PasteAsync_WithFileDropList_ShouldDeserializeJsonPaths()
    {
        var writer = new Mock<IClipboardWriter>();
        writer.Setup(w => w.TrySetAsync(It.IsAny<ClipboardPayload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var trigger = new Mock<IPasteTrigger>();
        trigger.Setup(t => t.TriggerPasteAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var window = new Mock<IMainWindowController>();
        window.Setup(w => w.HideMainWindowAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var pasteTarget = new Mock<IPasteTargetWindowService>();
        pasteTarget.Setup(p => p.WaitForReadyToPasteAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (nint)0));

        var logger = new Mock<ILogger>();

        var useCase = new ClipboardPasteUseCase(writer.Object, trigger.Object, window.Object, pasteTarget.Object, logger.Object);
        var paths = new[] { @"C:\a.txt", @"C:\b.doc" };
        var item = new ClipboardItem
        {
            ContentType = ClipboardContentTypes.FileDropList,
            Content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(paths)),
            CreatedAt = DateTime.Now
        };

        await useCase.PasteAsync(item);

        writer.Verify(w => w.TrySetAsync(
            It.Is<ClipboardPayload>(p =>
                p.Type == ClipboardPayloadType.FileDropList &&
                p.FilePaths != null &&
                p.FilePaths.SequenceEqual(paths)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PasteAsync_WithImage_ShouldWritePngBytes()
    {
        var writer = new Mock<IClipboardWriter>();
        writer.Setup(w => w.TrySetAsync(It.IsAny<ClipboardPayload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var trigger = new Mock<IPasteTrigger>();
        trigger.Setup(t => t.TriggerPasteAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var window = new Mock<IMainWindowController>();
        window.Setup(w => w.HideMainWindowAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var pasteTarget = new Mock<IPasteTargetWindowService>();
        pasteTarget.Setup(p => p.WaitForReadyToPasteAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (nint)0));

        var logger = new Mock<ILogger>();

        var useCase = new ClipboardPasteUseCase(writer.Object, trigger.Object, window.Object, pasteTarget.Object, logger.Object);
        var bytes = new byte[] { 1, 2, 3 };
        var item = new ClipboardItem { ContentType = ClipboardContentTypes.Image, Content = bytes, CreatedAt = DateTime.Now };

        await useCase.PasteAsync(item);

        writer.Verify(w => w.TrySetAsync(
            It.Is<ClipboardPayload>(p => p.Type == ClipboardPayloadType.ImagePng && ReferenceEquals(p.ImagePngBytes, bytes)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PasteAsync_WhenForegroundRestoreTimesOut_ShouldStillTriggerPaste()
    {
        var writer = new Mock<IClipboardWriter>();
        writer.Setup(w => w.TrySetAsync(It.IsAny<ClipboardPayload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var trigger = new Mock<IPasteTrigger>();
        trigger.Setup(t => t.TriggerPasteAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var window = new Mock<IMainWindowController>();
        window.Setup(w => w.HideMainWindowAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var pasteTarget = new Mock<IPasteTargetWindowService>();
        pasteTarget.SetupGet(p => p.PasteTargetWindowHandle).Returns(new nint(123));
        pasteTarget.Setup(p => p.WaitForReadyToPasteAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, new nint(456)));

        var logger = new Mock<ILogger>();

        var useCase = new ClipboardPasteUseCase(writer.Object, trigger.Object, window.Object, pasteTarget.Object, logger.Object);
        var item = new ClipboardItem { ContentType = ClipboardContentTypes.Text, Content = Encoding.UTF8.GetBytes("hello"), CreatedAt = DateTime.Now };

        await useCase.PasteAsync(item);

        trigger.Verify(t => t.TriggerPasteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
