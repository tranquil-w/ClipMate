using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ClipMate.Behaviors;

/// <summary>
/// ListView项点击行为
/// 用于在XAML中将ListView项点击事件绑定到ViewModel的Command
/// </summary>
public class ListViewItemClickBehavior : Behavior<ListView>
{
    public static readonly DependencyProperty ItemClickCommandProperty =
        DependencyProperty.Register(
            nameof(ItemClickCommand),
            typeof(ICommand),
            typeof(ListViewItemClickBehavior));

    public static readonly DependencyProperty ClearSelectionOnBlankClickProperty =
        DependencyProperty.Register(
            nameof(ClearSelectionOnBlankClick),
            typeof(bool),
            typeof(ListViewItemClickBehavior),
            new PropertyMetadata(true));

    public ICommand ItemClickCommand
    {
        get => (ICommand)GetValue(ItemClickCommandProperty);
        set => SetValue(ItemClickCommandProperty, value);
    }

    public bool ClearSelectionOnBlankClick
    {
        get => (bool)GetValue(ClearSelectionOnBlankClickProperty);
        set => SetValue(ClearSelectionOnBlankClickProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.MouseLeftButtonUp += OnMouseLeftButtonUp;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.MouseLeftButtonUp -= OnMouseLeftButtonUp;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListView listView)
            return;

        var point = e.GetPosition(listView);
        var element = listView.InputHitTest(point) as DependencyObject;
        var clickedItem = FindAncestor<ListViewItem>(element);

        if (clickedItem == null)
        {
            // 点击空白区域
            if (ClearSelectionOnBlankClick)
            {
                listView.SelectedItem = null;
            }
        }
        else
        {
            // 点击列表项，执行命令
            var item = clickedItem.DataContext ?? listView.SelectedItem;
            if (item != null && !Equals(listView.SelectedItem, item))
            {
                listView.SelectedItem = item;
            }

            if (ItemClickCommand?.CanExecute(item) == true)
            {
                ItemClickCommand.Execute(item);
            }
        }
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T dependencyObject)
                return dependencyObject;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
