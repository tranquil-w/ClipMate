using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ClipMate.Avalonia;
using ClipMate.Avalonia.Infrastructure;
using ClipMate.ViewModels;

namespace ClipMate.Avalonia.Views;

public partial class ClipboardView : UserControl
{
    private bool _isSubscribed;

    public ClipboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_isSubscribed)
        {
            return;
        }

        if (DataContext is ClipboardViewModel viewModel)
        {
            viewModel.SearchBoxFocusRequested += OnSearchBoxFocusRequested;
            viewModel.ScrollToSelectedRequested += OnScrollToSelectedRequested;
            _isSubscribed = true;
        }
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (!_isSubscribed)
        {
            return;
        }

        if (DataContext is ClipboardViewModel viewModel)
        {
            viewModel.SearchBoxFocusRequested -= OnSearchBoxFocusRequested;
            viewModel.ScrollToSelectedRequested -= OnScrollToSelectedRequested;
        }

        _isSubscribed = false;
    }

    private void OnSearchBoxFocusRequested(object? sender, EventArgs e)
    {
        ActivateForInput();
        SearchBox.Focus();
        SearchBox.SelectionStart = SearchBox.Text?.Length ?? 0;
    }

    private void OnScrollToSelectedRequested(object? sender, EventArgs e)
    {
        ScrollToSelectedItem();
    }

    private void ScrollToSelectedItem()
    {
        if (ListBox.SelectedItem != null)
        {
            ListBox.ScrollIntoView(ListBox.SelectedItem);
        }
    }

    private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ClipboardViewModel viewModel)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            if (!string.IsNullOrEmpty(SearchBox.Text))
            {
                SearchBox.Text = string.Empty;
                e.Handled = true;
                return;
            }

            ResumeNoActivate();
            if (TopLevel.GetTopLevel(this) is Window window)
            {
                window.Close();
            }
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            ResumeNoActivate();
            if (viewModel.SelectedItem != null)
            {
                viewModel.PasteCommand.Execute(viewModel.SelectedItem);
                e.Handled = true;
            }
            return;
        }

        if (e.Key == Key.Down || e.Key == Key.Up)
        {
            viewModel.SelectRelative(e.Key == Key.Up ? -1 : 1);
            ScrollToSelectedItem();
            e.Handled = true;
        }
    }

    private void SearchBox_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ActivateForInput();
        SearchBox.Focus();
    }

    private void SearchBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        ActivateForInput();
    }

    private void ListBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is ClipboardViewModel viewModel && e.Key == Key.Enter)
        {
            if (viewModel.SelectedItem != null)
            {
                viewModel.PasteCommand.Execute(viewModel.SelectedItem);
                e.Handled = true;
            }
        }
    }

    private void ListBox_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is ClipboardViewModel viewModel && viewModel.SelectedItem != null)
        {
            viewModel.PasteCommand.Execute(viewModel.SelectedItem);
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not ClipboardViewModel viewModel)
        {
            return;
        }

        var expectedKey = KeyMapping.ToAvaloniaKey(viewModel.FavoriteFilterHotKeyKey);
        var expectedModifiers = KeyMapping.ToAvaloniaModifiers(viewModel.FavoriteFilterHotKeyModifiers);
        if (e.Key == expectedKey && e.KeyModifiers == expectedModifiers)
        {
            viewModel.ToggleFavoriteFilterCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void ActivateForInput()
    {
        if (TopLevel.GetTopLevel(this) is MainWindow mainWindow)
        {
            if (!mainWindow.NoActivateWindowController.IsNoActivateSuspended)
            {
                mainWindow.NoActivateWindowController.SuspendNoActivate();
                mainWindow.Activate();
            }
        }
    }

    private void ResumeNoActivate()
    {
        if (TopLevel.GetTopLevel(this) is MainWindow mainWindow)
        {
            mainWindow.NoActivateWindowController.ResumeNoActivate();
        }
    }
}
