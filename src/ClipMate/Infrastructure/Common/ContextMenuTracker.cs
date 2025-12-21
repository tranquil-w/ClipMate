using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ClipMate.Infrastructure
{
    /// <summary>
    /// 全局追踪 ContextMenu 状态，用于防止关闭菜单时触发窗口拖动
    /// </summary>
    public static class ContextMenuTracker
    {
        private static bool _isInitialized;

        /// <summary>
        /// 是否有 ContextMenu 正在显示或刚刚关闭
        /// </summary>
        public static bool IsContextMenuOpen { get; private set; }

        /// <summary>
        /// 初始化追踪器（在应用启动时调用一次）
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            EventManager.RegisterClassHandler(
                typeof(ContextMenu),
                ContextMenu.OpenedEvent,
                new RoutedEventHandler(OnContextMenuOpened));

            EventManager.RegisterClassHandler(
                typeof(ContextMenu),
                ContextMenu.ClosedEvent,
                new RoutedEventHandler(OnContextMenuClosed));
        }

        private static void OnContextMenuOpened(object sender, RoutedEventArgs e)
        {
            IsContextMenuOpen = true;
        }

        private static void OnContextMenuClosed(object sender, RoutedEventArgs e)
        {
            // 延迟重置标志，确保点击事件处理时标志仍为 true
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() => IsContextMenuOpen = false),
                DispatcherPriority.Input);
        }

        /// <summary>
        /// 检查点击的元素是否是滚动条或其子元素
        /// </summary>
        /// <param name="originalSource">点击的原始元素</param>
        /// <returns>如果是滚动条或其子元素返回 true</returns>
        public static bool IsScrollBarOrChild(DependencyObject? originalSource)
        {
            if (originalSource == null)
                return false;

            // 向上遍历可视化树，查找ScrollBar
            var parent = originalSource;
            while (parent != null)
            {
                if (parent is System.Windows.Controls.Primitives.ScrollBar)
                {
                    return true;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }

            return false;
        }
    }
}