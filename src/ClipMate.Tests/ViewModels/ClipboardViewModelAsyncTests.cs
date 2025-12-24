using ClipMate.Core.Models;
using ClipMate.Core.Search;
using ClipMate.Platform.Abstractions.Clipboard;
using ClipMate.Presentation.Clipboard;
using ClipMate.Service.Clipboard;
using ClipMate.Service.Interfaces;
using ClipMate.Services;
using ClipMate.Tests.TestHelpers;
using ClipMate.ViewModels;
using Moq;
using Serilog;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace ClipMate.Tests.ViewModels;

public sealed class ClipboardViewModelAsyncTests : TestBase
{
    private readonly Mock<IClipboardService> _clipboardMock = new();
    private readonly FakeClipboardChangeSource _clipboardChangeSource = new();
    private readonly Mock<IClipboardCaptureUseCase> _captureUseCaseMock = new();
    private readonly Mock<IClipboardHistoryUseCase> _historyUseCaseMock = new();
    private readonly Mock<ISettingsService> _settingsMock = new();
    private readonly Mock<ILogger> _loggerMock = new();

    public ClipboardViewModelAsyncTests()
    {
        _settingsMock.Setup(s => s.GetClipboardItemMaxHeight()).Returns(100);
        _clipboardMock
            .Setup(s => s.Create(It.IsAny<ClipboardItem>()))
            .Returns<ClipboardItem>(CreateClipboardContent);
    }

    private ClipboardViewModel CreateViewModel()
    {
        return new ClipboardViewModel(
            _clipboardMock.Object,
            _clipboardChangeSource,
            _captureUseCaseMock.Object,
            _historyUseCaseMock.Object,
            _settingsMock.Object,
            new ImmediateUiDispatcher(),
            _loggerMock.Object);
    }

    private static void SeedItems(ClipboardViewModel viewModel, params IClipboardContent[] items)
    {
        viewModel.SeedItemsForTest(items.ToList());
    }

    [Fact]
    public async Task Constructor_ShouldNotBlockOnHistoryLoad()
    {
        await TestHost.SwitchToAppThread();

        var tcs = new TaskCompletionSource<IReadOnlyList<ClipboardItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _historyUseCaseMock
            .Setup(h => h.GetAllDescAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        _ = Task.Run(async () =>
        {
            await Task.Delay(800);
            tcs.TrySetResult(Array.Empty<ClipboardItem>());
        });

        var stopwatch = Stopwatch.StartNew();
        _ = CreateViewModel();
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed < TimeSpan.FromMilliseconds(300),
            $"构造函数耗时 {stopwatch.Elapsed.TotalMilliseconds}ms，应该显著小于历史加载延迟");
    }

    [Fact]
    public async Task LoadHistoryAsync_ShouldMergeWithExistingItems()
    {
        await TestHost.SwitchToAppThread();

        var tcs = new TaskCompletionSource<IReadOnlyList<ClipboardItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _historyUseCaseMock
            .Setup(h => h.GetAllDescAsync(It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var viewModel = CreateViewModel();

        var existingItem = new ClipboardItem
        {
            Id = 1,
            ContentType = ClipboardContentTypes.Text,
            Content = Encoding.UTF8.GetBytes("existing"),
            CreatedAt = DateTime.UtcNow,
            IsFavorite = false
        };
        SeedItems(viewModel, _clipboardMock.Object.Create(existingItem));

        var historyItems = new[]
        {
            existingItem,
            new ClipboardItem
            {
                Id = 2,
                ContentType = ClipboardContentTypes.Text,
                Content = Encoding.UTF8.GetBytes("new"),
                CreatedAt = DateTime.UtcNow,
                IsFavorite = false
            }
        };

        tcs.SetResult(historyItems);

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (DateTime.UtcNow < deadline && viewModel.ClipboardItems.Count < 2)
        {
            await Task.Delay(20);
        }

        Assert.Equal(2, viewModel.ClipboardItems.Count);

        var ids = viewModel.ClipboardItems.Select(i => i.Value.Id).ToHashSet();
        Assert.True(ids.SetEquals(new[] { 1, 2 }));
    }

    [Fact]
    public async Task LoadHistoryAsync_WhenFails_ShouldNotCrash()
    {
        await TestHost.SwitchToAppThread();

        _historyUseCaseMock
            .Setup(h => h.GetAllDescAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromException<IReadOnlyList<ClipboardItem>>(new InvalidOperationException("db error")));

        var viewModel = CreateViewModel();

        await Task.Delay(200);

        Assert.NotNull(viewModel.ClipboardItems);
        Assert.Empty(viewModel.ClipboardItems);
    }

    private static IClipboardContent CreateClipboardContent(ClipboardItem item)
    {
        var mock = new Mock<IClipboardContent>();
        mock.SetupGet(c => c.Value).Returns(item);
        mock.SetupGet(c => c.IsFavorite).Returns(item.IsFavorite);
        mock.Setup(c => c.IsVisible(It.IsAny<SearchQuerySnapshot>())).Returns(true);
        mock.SetupGet(c => c.Summary).Returns(item.ContentType == ClipboardContentTypes.Text
            ? Encoding.UTF8.GetString(item.Content)
            : item.ContentType);
        return mock.Object;
    }
}
