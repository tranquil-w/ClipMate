using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ClipMate.Avalonia.Infrastructure;
using ClipMate.ViewModels;

namespace ClipMate.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow(
        ClipboardViewModel clipboardViewModel,
        MainWindowViewModel mainWindowViewModel)
    {
        InitializeComponent();

        ClipboardView.DataContext = clipboardViewModel;
        DataContext = mainWindowViewModel;

        NoActivateWindowController = new NoActivateWindowController(this);
        NoActivateWindowController.Attach();

        PointerPressed += OnPointerPressed;
        Closing += OnClosing;
    }

    public NoActivateWindowController NoActivateWindowController { get; }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        NoActivateWindowController.ResumeNoActivate();
        Hide();
        e.Cancel = true;
    }
}
