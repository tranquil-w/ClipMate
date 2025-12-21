using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClipMate.Infrastructure;
using ClipMate.Service.Interfaces;
using ClipMate.Core.Models;
using ClipMate.Presentation.Clipboard;
using ClipMate.Services;
using ClipMate.Tests.TestHelpers;
using Moq;
using Serilog;
using ClipMate.Service.Clipboard;
using ClipMate.Platform.Abstractions.Clipboard;

namespace ClipMate.Tests.Services
{
    /// <summary>
    /// ClipboardService 的关键路径单元测试，覆盖文本/图片创建与粘贴流程，使用 Moq 模拟外部依赖。
    /// </summary>
	    public class ClipboardServiceTests : TestBase
	    {
	        private readonly Mock<ILogger> _loggerMock = new();
	        private readonly Mock<ISettingsService> _settingsServiceMock = new();
            private readonly Mock<IClipboardWriter> _clipboardWriterMock = new();
	        private readonly Mock<IClipboardPasteUseCase> _pasteUseCaseMock = new();
	        private readonly ClipboardService _service;

	        public ClipboardServiceTests()
	        {
	            _settingsServiceMock.Setup(s => s.GetClipboardItemMaxHeight()).Returns(100);
                _clipboardWriterMock
                    .Setup(w => w.TrySetAsync(It.IsAny<ClipboardPayload>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(true);
	            _service = new ClipboardService(
	                _settingsServiceMock.Object,
                    _clipboardWriterMock.Object,
                    _pasteUseCaseMock.Object,
	                _loggerMock.Object);
	        }

        /// <summary>
        /// 从数据库项走文本分支，确保内容被正确解码并返回 TextClipboard。
        /// </summary>
        [Fact]
        public void Create_WithTextItem_ShouldReturnTextClipboard()
        {
            var item = BuildTextItem("测试文本");
            var result = _service.Create(item);

            var clipboard = Assert.IsType<TextClipboard>(result);
            Assert.Same(item, clipboard.Value);               // 原始引用应被保留
            Assert.Equal("测试文本", clipboard.TextContent);    // 字节需被 UTF8 解码
            Assert.Equal(Constants.Text, clipboard.Value.ContentType);
        }

        /// <summary>
        /// 从数据库项走图片分支，验证字节流能被解析为 BitmapSource。
        /// </summary>
        [Fact]
        public async Task Create_WithImageItem_ShouldReturnImageClipboard()
        {
            await TestHost.SwitchToAppThread(); // WPF 图像对象需要 UI 线程
            var item = BuildImageItem();

            var result = _service.Create(item);

            var clipboard = Assert.IsType<ImageClipboard>(result);
            Assert.Same(item, clipboard.Value);
            Assert.Equal(Constants.Image, clipboard.Value.ContentType);
            Assert.NotNull(clipboard.ImageContent);           // 解码出的位图应存在
        }

        /// <summary>
        /// 传入未知的内容类型，应记录错误并抛出 NotSupportedException。
        /// </summary>
        [Fact]
        public void Create_WithUnsupportedItem_ShouldLogAndThrow()
        {
            var item = new ClipboardItem
            {
                Id = 99,
                ContentType = "Unknown",
                Content = Encoding.UTF8.GetBytes("X"),
                CreatedAt = DateTime.Now
            };

            Assert.Throws<NotSupportedException>(() => _service.Create(item));
            _loggerMock.Verify(
                l => l.Error(
                    It.IsAny<Exception>(),
                    It.Is<string>(msg => msg.Contains("创建剪贴板内容失败")),
                    It.IsAny<string>(),
                    It.IsAny<int>()),
                Times.Once);
        }

        /// <summary>
        /// 从对象创建文本内容，需构建新的 ClipboardItem。
        /// </summary>
        [Fact]
        public void Create_FromObject_Text_ShouldBuildItem()
        {
            var result = _service.Create((object)"行内文本");

            var clipboard = Assert.IsType<TextClipboard>(result);
            Assert.Equal(Constants.Text, clipboard.Value.ContentType);
            Assert.Equal("行内文本", clipboard.TextContent);
        }

        /// <summary>
        /// 从对象创建图片内容，需编码为字节并返回 ImageClipboard。
        /// </summary>
        [Fact]
        public async Task Create_FromObject_Image_ShouldBuildItem()
        {
            await TestHost.SwitchToAppThread();
            var bitmap = CreateTestBitmap();

            var result = _service.Create((object)bitmap);

            var clipboard = Assert.IsType<ImageClipboard>(result);
            Assert.Equal(Constants.Image, clipboard.Value.ContentType);
            Assert.NotNull(clipboard.ImageContent);
        }

        /// <summary>
        /// 给出不受支持的对象类型，应记录错误并抛出 NotSupportedException。
        /// </summary>
        [Fact]
        public void Create_FromObject_Unsupported_ShouldLogAndThrow()
        {
            var unsupported = new { Name = "匿名类型" };

            Assert.Throws<NotSupportedException>(() => _service.Create((object)unsupported));
            _loggerMock.Verify(
                l => l.Error(
                    It.IsAny<Exception>(),
                    It.Is<string>(msg => msg.Contains("从对象创建剪贴板内容失败")),
                    It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// 粘贴流程应委托给 IClipboardPasteUseCase 并记录日志。
        /// </summary>
        [Fact]
	        public async Task PasteAsync_ShouldDelegateToPasteUseCase()
	        {
                var item = BuildTextItem("测试粘贴内容");
	            var clipboardContentMock = new Mock<IClipboardContent>();
                clipboardContentMock.SetupGet(c => c.Value).Returns(item);

	            await _service.PasteAsync(clipboardContentMock.Object);

                _pasteUseCaseMock.Verify(u => u.PasteAsync(item, It.IsAny<CancellationToken>()), Times.Once);

            _loggerMock.Verify(l => l.Information(It.Is<string>(msg => msg.Contains("开始执行粘贴操作"))), Times.Once);
            _loggerMock.Verify(l => l.Information(It.Is<string>(msg => msg.Contains("粘贴操作完成"))), Times.Once);
	        }

        /// <summary>
        /// 从数据库项走文件列表分支，确保内容被正确反序列化并返回 FileDropListClipboard。
        /// </summary>
        [Fact]
        public void Create_WithFileDropListItem_ShouldReturnFileDropListClipboard()
        {
            var filePaths = new[] { @"C:\test\file1.txt", @"C:\test\file2.docx" };
            var item = BuildFileDropListItem(filePaths);
            var result = _service.Create(item);

            var clipboard = Assert.IsType<FileDropListClipboard>(result);
            Assert.Same(item, clipboard.Value);
            Assert.Equal(2, clipboard.FilePathList.Count);
            Assert.Equal(Constants.FileDropList, clipboard.Value.ContentType);
        }

        /// <summary>
        /// 从 StringCollection 对象创建文件列表内容，需构建新的 ClipboardItem。
        /// </summary>
        [Fact]
        public void Create_FromObject_FileDropList_ShouldBuildItem()
        {
            var collection = new StringCollection { @"C:\test\file1.txt", @"C:\test\file2.docx" };

            var result = _service.Create((object)collection);

            var clipboard = Assert.IsType<FileDropListClipboard>(result);
            Assert.Equal(Constants.FileDropList, clipboard.Value.ContentType);
            Assert.Equal(2, clipboard.FilePathList.Count);
        }

        /// <summary>
        /// 构造基础文本项，便于覆盖 Text 分支。
        /// </summary>
        private static ClipboardItem BuildTextItem(string text = "剪贴板文本")
        {
            return new ClipboardItem
            {
                Id = 1,
                ContentType = Constants.Text,
                Content = Encoding.UTF8.GetBytes(text),
                CreatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// 构造带图片内容的 ClipboardItem，使用 PNG 编码保证与 ImageClipboardFactory 兼容。
        /// </summary>
        private static ClipboardItem BuildImageItem()
        {
            var bitmap = CreateTestBitmap();
            using var ms = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(ms);

            return new ClipboardItem
            {
                Id = 2,
                ContentType = Constants.Image,
                Content = ms.ToArray(),
                CreatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// 构造基础文件列表项，便于覆盖 FileDropList 分支。
        /// </summary>
        private static ClipboardItem BuildFileDropListItem(string[] filePaths)
        {
            string json = JsonSerializer.Serialize(filePaths);
            return new ClipboardItem
            {
                Id = 3,
                ContentType = Constants.FileDropList,
                Content = Encoding.UTF8.GetBytes(json),
                CreatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// 生成一个 2x2 的小位图，包含不同像素以便验证解码路径。
        /// </summary>
        private static BitmapSource CreateTestBitmap()
        {
            var format = PixelFormats.Bgra32;
            const int width = 2;
            const int height = 2;
            int stride = width * (format.BitsPerPixel / 8);
            var pixels = new byte[stride * height];

            // 填充两个像素：蓝色与绿色，用于确保非空图像
            pixels[0] = 255;           // B
            pixels[1] = 0;             // G
            pixels[2] = 0;             // R
            pixels[3] = 255;           // A

            pixels[stride + 0] = 0;    // B
            pixels[stride + 1] = 255;  // G
            pixels[stride + 2] = 0;    // R
            pixels[stride + 3] = 255;  // A

            var bitmap = new WriteableBitmap(width, height, 96, 96, format, null);
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
            return bitmap;
        }
    }
}
