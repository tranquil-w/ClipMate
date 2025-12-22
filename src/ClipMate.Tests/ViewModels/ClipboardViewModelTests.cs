using ClipMate.Infrastructure;
using ClipMate.Service.Interfaces;
using ClipMate.Core.Models;
using ClipMate.Presentation.Clipboard;
using ClipMate.Services;
using ClipMate.Platform.Abstractions.Clipboard;
using ClipMate.Core.Search;
using ClipMate.Service.Clipboard;
using ClipMate.Tests.TestHelpers;
using ClipMate.ViewModels;
using Moq;
using Serilog;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace ClipMate.Tests.ViewModels
{
    /// <summary>
    /// ClipboardViewModel 的单元测试，覆盖窗口显示、相对选择、搜索文本操作等功能
    /// </summary>
    public class ClipboardViewModelTests : TestBase
    {
        private readonly Mock<IClipboardService> _clipboardMock = new();
        private readonly FakeClipboardChangeSource _clipboardChangeSource = new();
        private readonly Mock<IClipboardCaptureUseCase> _captureUseCaseMock = new();
        private readonly Mock<IClipboardHistoryUseCase> _historyUseCaseMock = new();
        private readonly Mock<ISettingsService> _settingsMock = new();
        private readonly Mock<ILogger> _loggerMock = new();

        public ClipboardViewModelTests()
        {
            _settingsMock.Setup(s => s.GetClipboardItemMaxHeight()).Returns(100);
            _historyUseCaseMock
                .Setup(h => h.GetAllDescAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ClipboardItem>());
        }

        private ClipboardViewModel CreateViewModel()
        {
            return new ClipboardViewModel(
                _clipboardMock.Object,
                _clipboardChangeSource,
                _captureUseCaseMock.Object,
                _historyUseCaseMock.Object,
                _settingsMock.Object,
                _loggerMock.Object);
        }

        #region OnWindowShown Tests

        [Fact]
        public async Task OnWindowShown_WhenItemsExist_ShouldSelectFirstItem()
        {
            await TestHost.SwitchToAppThread();

            // Arrange
            var viewModel = CreateViewModel();
            var item1 = CreateMockClipboardContent("Item1");
            var item2 = CreateMockClipboardContent("Item2");
            viewModel.ClipboardItems.Add(item1.Object);
            viewModel.ClipboardItems.Add(item2.Object);

            // Act
            viewModel.OnWindowShown();

            // Assert
            Assert.Same(item1.Object, viewModel.SelectedItem);
        }

        [Fact]
        public async Task OnWindowShown_WhenNoItems_ShouldNotThrow()
        {
            await TestHost.SwitchToAppThread();

            // Arrange
            var viewModel = CreateViewModel();

            // Act
            var exception = Record.Exception(() => viewModel.OnWindowShown());

            // Assert
            Assert.Null(exception);
            Assert.Null(viewModel.SelectedItem);
        }

        [Fact]
        public async Task OnWindowShown_WithFilter_ShouldSelectFirstVisibleItem()
        {
            await TestHost.SwitchToAppThread();

            // Arrange
            var viewModel = CreateViewModel();
            var item1 = CreateMockClipboardContent("Hidden", isFavorite: false);
            var item2 = CreateMockClipboardContent("Favorite", isFavorite: true);
            viewModel.ClipboardItems.Add(item1.Object);
            viewModel.ClipboardItems.Add(item2.Object);

            // 启用收藏过滤
            viewModel.IsFavoriteFilterEnabled = true;

            // 等待过滤刷新
            await Task.Delay(300);

            // Act
            viewModel.OnWindowShown();

            // Assert - 应该选中第一个可见项（收藏项）
            Assert.Same(item2.Object, viewModel.SelectedItem);
        }

        #endregion

        #region SelectRelative Tests

        [Fact]
        public async Task SelectRelative_Down_ShouldSelectNextItem()
        {
            await TestHost.SwitchToAppThread();

            // Arrange
            var viewModel = CreateViewModel();
            var item1 = CreateMockClipboardContent("Item1");
            var item2 = CreateMockClipboardContent("Item2");
            viewModel.ClipboardItems.Add(item1.Object);
            viewModel.ClipboardItems.Add(item2.Object);
            viewModel.SelectedItem = item1.Object;

            // Act
            viewModel.SelectRelative(1);

            // Assert
            Assert.Same(item2.Object, viewModel.SelectedItem);
        }

        [Fact]
        public async Task SelectRelative_Up_ShouldSelectPreviousItem()
        {
            await TestHost.SwitchToAppThread();

            // Arrange
            var viewModel = CreateViewModel();
            var item1 = CreateMockClipboardContent("Item1");
            var item2 = CreateMockClipboardContent("Item2");
            viewModel.ClipboardItems.Add(item1.Object);
            viewModel.ClipboardItems.Add(item2.Object);
            viewModel.SelectedItem = item2.Object;

            // Act
            viewModel.SelectRelative(-1);

            // Assert
            Assert.Same(item1.Object, viewModel.SelectedItem);
        }

        [Fact]
        public async Task SelectRelative_AtTop_ShouldStayAtTop()
        {
            await TestHost.SwitchToAppThread();

            // Arrange
            var viewModel = CreateViewModel();
            var item1 = CreateMockClipboardContent("Item1");
            var item2 = CreateMockClipboardContent("Item2");
            viewModel.ClipboardItems.Add(item1.Object);
            viewModel.ClipboardItems.Add(item2.Object);
            viewModel.SelectedItem = item1.Object;

            // Act
            viewModel.SelectRelative(-1);

            // Assert
            Assert.Same(item1.Object, viewModel.SelectedItem);
        }

        [Fact]
        public async Task SelectRelative_AtBottom_ShouldStayAtBottom()
        {
            await TestHost.SwitchToAppThread();

            // Arrange
            var viewModel = CreateViewModel();
            var item1 = CreateMockClipboardContent("Item1");
            var item2 = CreateMockClipboardContent("Item2");
            viewModel.ClipboardItems.Add(item1.Object);
            viewModel.ClipboardItems.Add(item2.Object);
            viewModel.SelectedItem = item2.Object;

            // Act
            viewModel.SelectRelative(1);

            // Assert
            Assert.Same(item2.Object, viewModel.SelectedItem);
        }

        [Fact]
        public async Task SelectRelative_WhenNoSelection_ShouldSelectFirst()
        {
            await TestHost.SwitchToAppThread();

            // Arrange
            var viewModel = CreateViewModel();
            var item1 = CreateMockClipboardContent("Item1");
            var item2 = CreateMockClipboardContent("Item2");
            viewModel.ClipboardItems.Add(item1.Object);
            viewModel.ClipboardItems.Add(item2.Object);
            viewModel.SelectedItem = null;

            // Act
            viewModel.SelectRelative(1);

            // Assert
            Assert.Same(item1.Object, viewModel.SelectedItem);
        }

        [Fact]
        public async Task SelectRelative_WhenNoItems_ShouldNotThrow()
        {
            await TestHost.SwitchToAppThread();

            // Arrange
            var viewModel = CreateViewModel();

            // Act
            var exception = Record.Exception(() => viewModel.SelectRelative(1));

            // Assert
            Assert.Null(exception);
        }

        #endregion

        #region BackspaceSearchText Tests

        [Fact]
        public async Task BackspaceSearchText_ShouldRemoveLastChar()
        {
            await TestHost.SwitchToAppThread();

            // Arrange
            var viewModel = CreateViewModel();
            viewModel.SearchQuery = "test";

            // Act
            viewModel.BackspaceSearchText();

            // Assert
            Assert.Equal("tes", viewModel.SearchQuery);
        }

        [Fact]
        public async Task BackspaceSearchText_SingleChar_ShouldClearQuery()
        {
            await TestHost.SwitchToAppThread();

            // Arrange
            var viewModel = CreateViewModel();
            viewModel.SearchQuery = "a";

            // Act
            viewModel.BackspaceSearchText();

            // Assert
            Assert.Equal(string.Empty, viewModel.SearchQuery);
        }

        [Fact]
        public async Task BackspaceSearchText_Empty_ShouldDoNothing()
        {
            await TestHost.SwitchToAppThread();

            // Arrange
            var viewModel = CreateViewModel();
            viewModel.SearchQuery = string.Empty;

            // Act
            var exception = Record.Exception(() => viewModel.BackspaceSearchText());

            // Assert
            Assert.Null(exception);
            Assert.Equal(string.Empty, viewModel.SearchQuery);
        }

        [Fact]
        public async Task BackspaceSearchText_MultipleTimes_ShouldRemoveMultipleChars()
        {
            await TestHost.SwitchToAppThread();

            // Arrange
            var viewModel = CreateViewModel();
            viewModel.SearchQuery = "hello";

            // Act
            viewModel.BackspaceSearchText();
            viewModel.BackspaceSearchText();
            viewModel.BackspaceSearchText();

            // Assert
            Assert.Equal("he", viewModel.SearchQuery);
        }

        #endregion

        #region Helper Methods

        private static Mock<IClipboardContent> CreateMockClipboardContent(
            string summary,
            bool isFavorite = false)
        {
            var mock = new Mock<IClipboardContent>();
            mock.Setup(c => c.Summary).Returns(summary);
            mock.SetupProperty(c => c.IsFavorite, isFavorite);
            mock.Setup(c => c.IsVisible(It.IsAny<SearchQuerySnapshot>())).Returns(true);
            mock.Setup(c => c.Value).Returns(new ClipboardItem
            {
                Id = 1,
                ContentType = Constants.Text,
                Content = System.Text.Encoding.UTF8.GetBytes(summary),
                CreatedAt = DateTime.Now,
                IsFavorite = isFavorite
            });
            return mock;
        }

        #endregion
    }
}
