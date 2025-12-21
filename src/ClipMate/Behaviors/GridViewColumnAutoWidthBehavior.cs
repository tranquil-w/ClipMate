using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace ClipMate.Behaviors;

/// <summary>
/// GridView 列自动宽度行为
/// 自动调整第一列的宽度以填充 ListView 的可用空间
/// </summary>
public class GridViewColumnAutoWidthBehavior : Behavior<ListView>
{
    /// <summary>
    /// 右侧边距，用于预留滚动条空间
    /// </summary>
    public static readonly DependencyProperty RightMarginProperty =
        DependencyProperty.Register(
            nameof(RightMargin),
            typeof(double),
            typeof(GridViewColumnAutoWidthBehavior),
            new PropertyMetadata(25.0));

    public double RightMargin
    {
        get => (double)GetValue(RightMarginProperty);
        set => SetValue(RightMarginProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.SizeChanged += OnSizeChanged;
        AssociatedObject.Loaded += OnLoaded;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.SizeChanged -= OnSizeChanged;
        AssociatedObject.Loaded -= OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateColumnWidth();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateColumnWidth();
    }

    private void UpdateColumnWidth()
    {
        if (AssociatedObject.View is not GridView gridView || gridView.Columns.Count == 0)
            return;

        var desiredWidth = AssociatedObject.ActualWidth - RightMargin;
        if (desiredWidth > 0)
        {
            gridView.Columns[0].Width = desiredWidth;
        }
    }
}
