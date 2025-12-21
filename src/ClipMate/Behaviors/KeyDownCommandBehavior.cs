using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace ClipMate.Behaviors;

/// <summary>
/// KeyDown事件到Command的Attached Behavior
/// 用于在XAML中将KeyDown事件绑定到ViewModel的Command
/// </summary>
public class KeyDownCommandBehavior : Behavior<FrameworkElement>
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(KeyDownCommandBehavior));

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.KeyDown += OnKeyDown;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.KeyDown -= OnKeyDown;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (Command?.CanExecute(e) == true)
        {
            Command.Execute(e);
        }
    }
}
