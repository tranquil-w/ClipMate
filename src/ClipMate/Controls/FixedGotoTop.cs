using System.Windows;
using ClipMate.Infrastructure;
using HandyControl.Controls;
using WpfScrollViewer = System.Windows.Controls.ScrollViewer;
using WpfScrollChangedEventArgs = System.Windows.Controls.ScrollChangedEventArgs;

namespace ClipMate.Controls;

/// <summary>
/// 修复 HandyControl GotoTop 在虚拟化 ListView 中 AutoHiding 不工作的问题。
/// 通过手动获取 Target 内部的 ScrollViewer 并监听滚动事件来控制显示/隐藏。
/// </summary>
public class FixedGotoTop : GotoTop
{
    private WpfScrollViewer? _scrollViewer;

    public FixedGotoTop()
    {
        // 禁用原生的 AutoHiding，我们自己处理
        AutoHiding = false;
        Visibility = Visibility.Collapsed;
        Loaded += OnLoaded;
        Click += OnClick;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 延迟执行以确保 Target 的模板已加载
        Dispatcher.BeginInvoke(new Action(SetupScrollViewer), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void SetupScrollViewer()
    {
        if (Target == null)
            return;

        _scrollViewer = VisualTreeExtensions.FindDescendant<WpfScrollViewer>(Target);
        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged += OnScrollChanged;
            // 初始检查
            UpdateVisibility();
        }
    }

    private void OnScrollChanged(object sender, WpfScrollChangedEventArgs e)
    {
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (_scrollViewer == null)
            return;

        Visibility = _scrollViewer.VerticalOffset >= HidingHeight
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        _scrollViewer?.ScrollToTop();
    }
}
