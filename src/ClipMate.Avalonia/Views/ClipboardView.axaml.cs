using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ClipMate.Avalonia;
using ClipMate.Avalonia.Infrastructure;
using ClipMate.ViewModels;

namespace ClipMate.Avalonia.Views;

public partial class ClipboardView : UserControl
{
    private const double ScrollBarHitWidth = 10;
    private bool _isSubscribed;
    private bool _isPointerDown;
    private bool _isDragging;
    private PixelPoint _dragStartScreenPoint;
    private PixelPoint _dragStartWindowPosition;

    public ClipboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

        ListBox.AddHandler(PointerPressedEvent, ListBox_PointerPressed, RoutingStrategies.Tunnel);
        ListBox.AddHandler(PointerMovedEvent, ListBox_PointerMoved, RoutingStrategies.Tunnel);
        ListBox.AddHandler(PointerReleasedEvent, ListBox_PointerReleased, RoutingStrategies.Tunnel);
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
        if (ListBox.SelectedItem == null)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (!ListBox.IsVisible ||
                ListBox.Bounds.Width <= 0 ||
                ListBox.Bounds.Height <= 0 ||
                ListBox.SelectedItem == null)
            {
                return;
            }

            try
            {
                ListBox.ScrollIntoView(ListBox.SelectedItem);
            }
            catch (InvalidOperationException)
            {
                // Ignore if layout is transiently invalid during show/refresh.
            }
        }, DispatcherPriority.Background);
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

    private void ListBox_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed == false)
        {
            return;
        }

        if (IsPointerOnScrollBar(e))
        {
            return;
        }

        if (TopLevel.GetTopLevel(this) is not Window window)
        {
            return;
        }

        _isPointerDown = true;
        _isDragging = false;

        var windowPoint = e.GetPosition(window);
        _dragStartScreenPoint = window.PointToScreen(windowPoint);
        _dragStartWindowPosition = window.Position;
    }

    private bool IsPointerOnScrollBar(PointerPressedEventArgs e)
    {
        var bounds = ListBox.Bounds;
        if (bounds.Width <= 0)
        {
            return false;
        }

        var point = e.GetPosition(ListBox);
        return point.X >= bounds.Width - ScrollBarHitWidth;
    }

    private void ListBox_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPointerDown)
        {
            return;
        }

        if (TopLevel.GetTopLevel(this) is not Window window)
        {
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isPointerDown = false;
            _isDragging = false;
            return;
        }

        var currentScreenPoint = window.PointToScreen(e.GetPosition(window));
        var deltaX = currentScreenPoint.X - _dragStartScreenPoint.X;
        var deltaY = currentScreenPoint.Y - _dragStartScreenPoint.Y;

        if (!_isDragging && (Math.Abs(deltaX) > 3 || Math.Abs(deltaY) > 3))
        {
            _isDragging = true;
        }

        if (_isDragging)
        {
            window.Position = new PixelPoint(
                _dragStartWindowPosition.X + deltaX,
                _dragStartWindowPosition.Y + deltaY);
        }
    }

    private void ListBox_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isPointerDown)
        {
            return;
        }

        _isPointerDown = false;

        if (_isDragging)
        {
            _isDragging = false;
            e.Handled = true;
            return;
        }

        if (e.InitialPressMouseButton != MouseButton.Left)
        {
            return;
        }

        if (DataContext is ClipboardViewModel viewModel && viewModel.SelectedItem != null)
        {
            viewModel.PasteCommand.Execute(viewModel.SelectedItem);
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
