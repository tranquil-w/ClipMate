using System.Windows;
using System.Windows.Media;

namespace ClipMate.Infrastructure;

/// <summary>
/// WPF 可视化树扩展方法
/// </summary>
public static class VisualTreeExtensions
{
    /// <summary>
    /// 向上遍历可视化树，查找指定类型的祖先元素
    /// </summary>
    public static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T ancestor)
            {
                return ancestor;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    /// <summary>
    /// 向下遍历可视化树，查找指定类型的后代元素
    /// </summary>
    public static T? FindDescendant<T>(DependencyObject? parent) where T : DependencyObject
    {
        if (parent == null)
            return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
                return typedChild;

            var result = FindDescendant<T>(child);
            if (result != null)
                return result;
        }
        return null;
    }
}
