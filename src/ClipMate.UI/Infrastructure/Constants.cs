using ClipMate.Core.Models;

namespace ClipMate.Infrastructure
{
    /// <summary>
    /// 应用程序常量
    /// </summary>
    public static class Constants
    {
        public const string AppName = "ClipMate";
        public const string ShowMainWindowHotkey = "ShowMainWindow";
        public const string Text = ClipboardContentTypes.Text;
        public const string Image = ClipboardContentTypes.Image;
        public const string FileDropList = ClipboardContentTypes.FileDropList;
    }

    /// <summary>
    /// 搜索相关常量
    /// </summary>
    public static class SearchConstants
    {
        /// <summary>
        /// 可搜索文本的最大长度（超过此长度将被截断用于搜索索引）
        /// </summary>
        public const int MaxSearchTextLength = 4096;
    }

    /// <summary>
    /// 显示相关常量
    /// </summary>
    public static class DisplayConstants
    {
        /// <summary>
        /// 文件名显示的最大长度
        /// </summary>
        public const int MaxFileNameLength = 20;
    }
}
