using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using ClipMate.Infrastructure;
using ClipMate.Core.Models;
using ClipMate.Tests.TestHelpers;
using ClipMate.Presentation.Clipboard;
using ClipMate.Core.Search;
using ClipMate.Platform.Abstractions.Clipboard;
using Moq;

namespace ClipMate.Tests.ViewModels
{
    /// <summary>
    /// FileDropListClipboard 和 FileDropListClipboardFactory 的单元测试。
    /// 覆盖文件剪贴板的创建、序列化、反序列化、搜索和显示功能。
    /// </summary>
    public class FileDropListClipboardTests : TestBase
    {
        private readonly Mock<IClipboardWriter> _clipboardWriterMock;
        private readonly FileDropListClipboardFactory _factory;
        private readonly string _tempDir;
        private readonly string _testFile1;
        private readonly string _testFile2;
        private readonly string _testSubDir;

        public FileDropListClipboardTests()
        {
            _clipboardWriterMock = new Mock<IClipboardWriter>();
            _factory = new FileDropListClipboardFactory(_clipboardWriterMock.Object);

            // 创建临时测试文件和目录
            _tempDir = Path.Combine(Path.GetTempPath(), $"ClipMateTest_{Guid.NewGuid()}");
            Directory.CreateDirectory(_tempDir);

            _testFile1 = Path.Combine(_tempDir, "测试文件.txt");
            _testFile2 = Path.Combine(_tempDir, "长文件名超过二十个字符的测试文件.docx");
            _testSubDir = Path.Combine(_tempDir, "测试文件夹");

            File.WriteAllText(_testFile1, "测试内容");
            File.WriteAllText(_testFile2, "测试内容");
            Directory.CreateDirectory(_testSubDir);
        }

        ~FileDropListClipboardTests()
        {
            // 清理临时文件
            if (Directory.Exists(_tempDir))
            {
                try
                {
                    Directory.Delete(_tempDir, true);
                }
                catch
                {
                    // 忽略清理错误
                }
            }
        }

        #region Factory Create from ClipboardItem Tests

        /// <summary>
        /// 从 ClipboardItem 创建 FileDropListClipboard，验证反序列化成功。
        /// </summary>
        [Fact]
        public void Create_FromClipboardItem_ShouldDeserializeCorrectly()
        {
            var item = BuildFileDropListItem(new[] { _testFile1, _testFile2 });

            var result = _factory.Create(item);

            var clipboard = Assert.IsType<FileDropListClipboard>(result);
            Assert.Same(item, clipboard.Value);
            Assert.Equal(2, clipboard.FilePathList.Count);
            Assert.Contains(_testFile1, clipboard.FilePathList.Cast<string>());
            Assert.Contains(_testFile2, clipboard.FilePathList.Cast<string>());
        }

        /// <summary>
        /// 从空的文件列表创建，应返回空集合。
        /// </summary>
        [Fact]
        public void Create_FromEmptyArray_ShouldReturnEmptyCollection()
        {
            var item = BuildFileDropListItem([]);

            var result = _factory.Create(item);

            var clipboard = Assert.IsType<FileDropListClipboard>(result);
            Assert.Empty(clipboard.FilePathList);
        }

        /// <summary>
        /// 从包含空字符串的列表创建，应过滤掉空项。
        /// </summary>
        [Fact]
        public void Create_WithEmptyStrings_ShouldFilterThem()
        {
            var item = BuildFileDropListItem(new[] { _testFile1, "", "   ", _testFile2 });

            var result = _factory.Create(item);

            var clipboard = Assert.IsType<FileDropListClipboard>(result);
            Assert.Equal(2, clipboard.FilePathList.Count);
            Assert.DoesNotContain("", clipboard.FilePathList.Cast<string>());
        }

        /// <summary>
        /// 传入损坏的 JSON 数据，应抛出异常。
        /// </summary>
        [Fact]
        public void Create_WithInvalidJson_ShouldThrowException()
        {
            var item = new ClipboardItem
            {
                Id = 1,
                ContentType = Constants.FileDropList,
                Content = Encoding.UTF8.GetBytes("{ invalid json }"),
                CreatedAt = DateTime.Now
            };

            Assert.Throws<JsonException>(() => _factory.Create(item));
        }

        #endregion

        #region Factory Create from Object Tests

        /// <summary>
        /// 从 StringCollection 对象创建，验证序列化成功。
        /// </summary>
        [Fact]
        public void Create_FromStringCollection_ShouldSerializeCorrectly()
        {
            var collection = new StringCollection { _testFile1, _testFile2 };

            var result = _factory.Create(collection);

            var clipboard = Assert.IsType<FileDropListClipboard>(result);
            Assert.Equal(Constants.FileDropList, clipboard.Value.ContentType);
            Assert.Equal(2, clipboard.FilePathList.Count);

            // 验证可以反序列化
            string json = Encoding.UTF8.GetString(clipboard.Value.Content);
            var deserialized = JsonSerializer.Deserialize<string[]>(json);
            Assert.NotNull(deserialized);
            Assert.Equal(2, deserialized.Length);
        }

        /// <summary>
        /// 从 StringCollection 创建时过滤空字符串。
        /// </summary>
        [Fact]
        public void Create_FromStringCollectionWithEmpty_ShouldFilterThem()
        {
            var collection = new StringCollection { _testFile1, "", "   ", _testFile2 };

            var result = _factory.Create(collection);

            var clipboard = Assert.IsType<FileDropListClipboard>(result);
            Assert.Equal(2, clipboard.FilePathList.Count);
        }

        /// <summary>
        /// 传入非 StringCollection 类型对象，应抛出 NotSupportedException。
        /// </summary>
        [Fact]
        public void Create_FromUnsupportedType_ShouldThrow()
        {
            var unsupported = new List<string> { _testFile1 };

            Assert.Throws<NotSupportedException>(() => _factory.Create(unsupported));
        }

        #endregion

        #region Summary Property Tests

        /// <summary>
        /// 单个文件的 Summary 应显示文件名和文件图标。
        /// </summary>
        [Fact]
        public void Summary_SingleFile_ShouldShowFileName()
        {
            var collection = new StringCollection { _testFile1 };
            var clipboard = (FileDropListClipboard)_factory.Create(collection);

            var summary = clipboard.Summary;

            Assert.StartsWith("📄 ", summary);
            Assert.Contains("测试文件.txt", summary);
            Assert.DoesNotContain("+", summary);
        }

        /// <summary>
        /// 多个文件的 Summary 应显示第一个文件名和剩余文件数量。
        /// </summary>
        [Fact]
        public void Summary_MultipleFiles_ShouldShowCountWithPlus()
        {
            var collection = new StringCollection { _testFile1, _testFile2 };
            var clipboard = (FileDropListClipboard)_factory.Create(collection);

            var summary = clipboard.Summary;

            Assert.StartsWith("📄 ", summary);
            Assert.Contains("测试文件.txt", summary);
            Assert.Contains("(+1 个文件)", summary);
        }

        /// <summary>
        /// 文件夹的 Summary 应显示文件夹图标。
        /// </summary>
        [Fact]
        public void Summary_Directory_ShouldShowFolderIcon()
        {
            var collection = new StringCollection { _testSubDir };
            var clipboard = (FileDropListClipboard)_factory.Create(collection);

            var summary = clipboard.Summary;

            Assert.StartsWith("📁 ", summary);
            Assert.Contains("测试文件夹", summary);
        }

        /// <summary>
        /// 长文件名应该被截断并添加省略号。
        /// </summary>
        [Fact]
        public void Summary_LongFileName_ShouldTruncate()
        {
            var collection = new StringCollection { _testFile2 };
            var clipboard = (FileDropListClipboard)_factory.Create(collection);

            var summary = clipboard.Summary;

            Assert.StartsWith("📄 ", summary);
            Assert.Contains("...", summary);
            Assert.DoesNotContain("长文件名超过二十个字符的测试文件.docx", summary);
        }

        /// <summary>
        /// 空的文件列表应显示默认文本。
        /// </summary>
        [Fact]
        public void Summary_EmptyList_ShouldShowDefault()
        {
            var collection = new StringCollection();
            var item = new ClipboardItem
            {
                ContentType = Constants.FileDropList,
                Content = Encoding.UTF8.GetBytes("[]"),
                CreatedAt = DateTime.Now
            };
            var clipboard = new FileDropListClipboard(item, collection, _clipboardWriterMock.Object);

            var summary = clipboard.Summary;

            Assert.Equal("📄 文件", summary);
        }

        #endregion

        #region IsVisible Search Tests

        /// <summary>
        /// 空查询字符串应返回 true（显示所有项）。
        /// </summary>
        [Fact]
        public void IsVisible_EmptyQuery_ShouldReturnTrue()
        {
            var collection = new StringCollection { _testFile1 };
            var clipboard = (FileDropListClipboard)_factory.Create(collection);

            Assert.True(clipboard.IsVisible(Query("")));
            Assert.True(clipboard.IsVisible(SearchQuerySnapshot.Empty));
            Assert.True(clipboard.IsVisible(Query("   ")));
        }

        /// <summary>
        /// 按文件名搜索应返回匹配的文件。
        /// </summary>
        [Fact]
        public void IsVisible_SearchByFileName_ShouldMatch()
        {
            var collection = new StringCollection { _testFile1 };
            var clipboard = (FileDropListClipboard)_factory.Create(collection);

            Assert.True(clipboard.IsVisible(Query("测试文件")));
            Assert.True(clipboard.IsVisible(Query("测试")));
            Assert.True(clipboard.IsVisible(Query("文件")));
        }

        /// <summary>
        /// 按扩展名搜索应返回匹配的文件（支持带点和不带点）。
        /// </summary>
        [Fact]
        public void IsVisible_SearchByExtension_ShouldMatch()
        {
            var collection = new StringCollection { _testFile1, _testFile2 };
            var clipboard = (FileDropListClipboard)_factory.Create(collection);

            Assert.True(clipboard.IsVisible(Query("txt")));
            Assert.True(clipboard.IsVisible(Query(".txt")));
            Assert.True(clipboard.IsVisible(Query("docx")));
            Assert.True(clipboard.IsVisible(Query(".DOCX"))); // 不区分大小写
        }

        /// <summary>
        /// 按完整路径搜索应返回匹配的文件。
        /// </summary>
        [Fact]
        public void IsVisible_SearchByFullPath_ShouldMatch()
        {
            var collection = new StringCollection { _testFile1 };
            var clipboard = (FileDropListClipboard)_factory.Create(collection);

            Assert.True(clipboard.IsVisible(Query("ClipMateTest")));
            Assert.True(clipboard.IsVisible(Query(_tempDir)));
        }

        /// <summary>
        /// 搜索不存在的内容应返回 false。
        /// </summary>
        [Fact]
        public void IsVisible_NoMatch_ShouldReturnFalse()
        {
            var collection = new StringCollection { _testFile1 };
            var clipboard = (FileDropListClipboard)_factory.Create(collection);

            Assert.False(clipboard.IsVisible(Query("不存在的文件")));
            Assert.False(clipboard.IsVisible(Query("xyz")));
        }

        /// <summary>
        /// 搜索应不区分大小写。
        /// </summary>
        [Fact]
        public void IsVisible_SearchIsCaseInsensitive()
        {
            var collection = new StringCollection { _testFile1 };
            var clipboard = (FileDropListClipboard)_factory.Create(collection);

            Assert.True(clipboard.IsVisible(Query("TXT")));
            Assert.True(clipboard.IsVisible(Query("Txt")));
            Assert.True(clipboard.IsVisible(Query(".TXT")));
        }

        #endregion

        #region Helper Methods

        private static SearchQuerySnapshot Query(string text) => SearchQuerySnapshot.From(text);

        /// <summary>
        /// 构造基础文件列表项，便于测试。
        /// </summary>
        private static ClipboardItem BuildFileDropListItem(string[] filePaths)
        {
            string json = JsonSerializer.Serialize(filePaths);
            return new ClipboardItem
            {
                Id = 1,
                ContentType = Constants.FileDropList,
                Content = Encoding.UTF8.GetBytes(json),
                CreatedAt = DateTime.Now
            };
        }

        #endregion
    }
}
