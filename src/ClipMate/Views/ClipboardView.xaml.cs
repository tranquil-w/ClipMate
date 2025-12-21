using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System;
using ClipMate.Infrastructure;
using InteropWindowStyle = ClipMate.Interop.WindowStyle;

namespace ClipMate.Views
{
    /// <summary>
    /// ClipboardView.xaml 的交互逻辑
    /// </summary>
    public partial class ClipboardView : UserControl
    {
        private Point _startScreenPoint;
        private Point _startWindowPos;
        private bool _isDragging;
        private Window? _window;
        private MainWindow? _mainWindow;
        private NoActivateWindowController? _noActivateController;
        private bool _isImeComposing;
        private ToolTip? _imeHintToolTip;
        private DateTimeOffset _lastImeHintAt = DateTimeOffset.MinValue;

        public ClipboardView()
        {
            InitializeComponent();

            PreviewKeyDown += ClipboardView_PreviewKeyDown;
            ListView.PreviewMouseLeftButtonDown += ListView_PreviewMouseLeftButtonDown;
            ListView.PreviewMouseMove += ListView_PreviewMouseMove;
            ListView.PreviewMouseLeftButtonUp += ListView_PreviewMouseLeftButtonUp;
            ListView.SelectionChanged += (_, _) => ScrollToSelectedItem();

            Loaded += ClipboardView_Loaded;
        }

        private void ClipboardView_Loaded(object sender, RoutedEventArgs e)
        {
            _mainWindow = Window.GetWindow(this) as MainWindow;
            _noActivateController = _mainWindow?.NoActivateWindowController;

            // 订阅 ViewModel 的焦点请求事件
            if (DataContext is ViewModels.ClipboardViewModel viewModel)
            {
                viewModel.SearchBoxFocusRequested += OnSearchBoxFocusRequested;
                viewModel.ScrollToSelectedRequested += OnScrollToSelectedRequested;
            }

            // IME 组合输入检测（用于在激活输入模式下避免误拦截 Enter/Esc/方向键）
            TextCompositionManager.AddPreviewTextInputStartHandler(SearchBox, (_, _) => _isImeComposing = true);
            TextCompositionManager.AddPreviewTextInputUpdateHandler(SearchBox, (_, _) => _isImeComposing = true);
            TextCompositionManager.AddTextInputHandler(SearchBox, (_, _) => _isImeComposing = false);
        }

        private void OnSearchBoxFocusRequested(object? sender, EventArgs e)     
        {
            // 激活窗口以支持输入
            ActivateForInput();
            // 触发搜索框焦点
            SearchBox.Focus();
            // 将光标移到末尾
            SearchBox.SelectionStart = SearchBox.Text?.Length ?? 0;
        }

        private void OnScrollToSelectedRequested(object? sender, EventArgs e)
        {
            ScrollToSelectedItem();
        }

        private void ScrollToSelectedItem()
        {
            if (ListView.SelectedItem == null)
            {
                return;
            }

            ListView.ScrollIntoView(ListView.SelectedItem);
        }

        private void ListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 每次点击时先重置拖动状态，防止之前的状态影响
            _window = null;
            _isDragging = false;

            // 如果右键菜单刚关闭，不启动拖动（用户可能是点击关闭菜单）
            if (ContextMenuTracker.IsContextMenuOpen)
            {
                return;
            }

            // 检查点击的元素是否是滚动条或其子元素
            if (ContextMenuTracker.IsScrollBarOrChild(e.OriginalSource as DependencyObject))
            {
                return;
            }

            _window = Window.GetWindow(this);
            if (_window != null)
            {
                _startScreenPoint = _window.PointToScreen(e.GetPosition(_window));
                _startWindowPos = new Point(_window.Left, _window.Top);
            }
        }

        private void ListView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _window != null)
            {
                Point currentScreenPoint = _window.PointToScreen(e.GetPosition(_window));
                Vector diff = _startScreenPoint - currentScreenPoint;

                // 如果鼠标移动超过3像素，开始拖动窗口
                if (!_isDragging && (Math.Abs(diff.X) > 3 || Math.Abs(diff.Y) > 3))
                {
                    _isDragging = true;
                }

                // 如果正在拖动，手动移动窗口
                if (_isDragging)
                {
                    _window.Left = _startWindowPos.X + (currentScreenPoint.X - _startScreenPoint.X);
                    _window.Top = _startWindowPos.Y + (currentScreenPoint.Y - _startScreenPoint.Y);
                }
            }
        }

        private void ListView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 如果正在拖动，阻止事件传递，避免触发点击命令
            if (_isDragging)
            {
                e.Handled = true;
            }

            _isDragging = false;
            _window = null;
        }

        private void ClipboardView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 如果焦点已经在搜索框上，不需要处理
            if (SearchBox.IsFocused)
                return;

            // 处理中文输入法（IME）
            if (e.Key == Key.ImeProcessed)
            {
                MaybeShowImeHint();
                SearchBox.Focus();
                return;
            }

            // 检查是否是需要处理的键
            if (IsTypableKey(e.Key) && !IsNavigationKey(e.Key) && !IsFunctionKey(e.Key))
            {
                // 忽略修饰键组合
                if (Keyboard.Modifiers != ModifierKeys.None)
                    return;

                // 切换到搜索框，不阻止事件传播，让该字符输入到搜索框
                SearchBox.Focus();
            }
        }

        /// <summary>
        /// 处理搜索框中的 Escape 和上下键
        /// </summary>
        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (_isImeComposing)
                {
                    // IME 组合期间优先交给 IME 处理（取消组合输入）
                    return;
                }

                if (!string.IsNullOrEmpty(SearchBox.Text))
                {
                    SearchBox.Text = string.Empty;
                    e.Handled = true;
                    return;
                }

                ResumeNoActivateMode();
                _mainWindow?.Close();
                e.Handled = true;
                return;
            }
            else if (e.Key == Key.Enter)
            {
                if (_isImeComposing)
                {
                    // IME 组合期间允许 Enter 由 IME 完成提交
                    return;
                }

                ResumeNoActivateMode();

                if (DataContext is ViewModels.ClipboardViewModel viewModel &&
                    viewModel.SelectedItem != null)
                {
                    viewModel.PasteCommand.Execute(viewModel.SelectedItem);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Down || e.Key == Key.Up)
            {
                if (_isImeComposing)
                {
                    return;
                }

                if (DataContext is ViewModels.ClipboardViewModel viewModel)
                {
                    viewModel.SelectRelative(e.Key == Key.Up ? -1 : 1);
                    if (viewModel.SelectedItem != null)
                    {
                        ListView.ScrollIntoView(viewModel.SelectedItem);
                    }
                    e.Handled = true;
                }
            }
        }

        private void MaybeShowImeHint()
        {
            if (DataContext is not ViewModels.ClipboardViewModel viewModel ||
                !viewModel.ImeHintsEnabled)
            {
                return;
            }

            var now = DateTimeOffset.UtcNow;
            if (now - _lastImeHintAt < TimeSpan.FromSeconds(2))
            {
                return;
            }

            _lastImeHintAt = now;

            _imeHintToolTip ??= new ToolTip
            {
                Content = "无焦点模式不支持输入法组合输入，点击搜索框进入输入模式",
                PlacementTarget = SearchBox,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Top,
                StaysOpen = false
            };

            _imeHintToolTip.IsOpen = true;
            _ = Dispatcher.InvokeAsync(async () =>
            {
                await System.Threading.Tasks.Task.Delay(1500);
                _imeHintToolTip.IsOpen = false;
            });
        }

        /// <summary>
        /// 搜索框鼠标按下事件 - 临时激活窗口以支持完整输入
        /// </summary>
        private void SearchBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ActivateForInput();
        }

        /// <summary>
        /// 搜索框获得焦点事件
        /// </summary>
        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            // 确保窗口处于激活模式
            ActivateForInput();
        }

        /// <summary>
        /// 搜索框失去焦点事件
        /// </summary>
        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // 当焦点转移到列表时，恢复无焦点模式
            // 注意：这里不立即恢复，因为用户可能还要继续操作
            // 只在窗口关闭或按 Enter/Esc 时恢复
        }

        /// <summary>
        /// 临时激活窗口以支持完整输入（IME、光标、方向键）
        /// </summary>
        private void ActivateForInput()
        {
            if (_noActivateController == null || _mainWindow == null)
            {
                return;
            }

            // 如果已经处于激活模式，无需操作
            if (_noActivateController.IsNoActivateSuspended)
            {
                return;
            }

            // 暂停无焦点模式
            _noActivateController.SuspendNoActivate();

            // 激活窗口
            var hwnd = new WindowInteropHelper(_mainWindow).Handle;
            if (hwnd != nint.Zero)
            {
                InteropWindowStyle.SetForegroundWindow(hwnd);
            }
        }

        /// <summary>
        /// 恢复无焦点模式
        /// </summary>
        private void ResumeNoActivateMode()
        {
            _noActivateController?.ResumeNoActivate();
        }

        /// <summary>
        /// 判断是否为导航键
        /// </summary>
        private static bool IsNavigationKey(Key key)
        {
            return key == Key.Up || key == Key.Down || key == Key.Left || key == Key.Right ||
                   key == Key.PageUp || key == Key.PageDown || key == Key.Home || key == Key.End ||
                   key == Key.Tab || key == Key.Enter;
        }

        /// <summary>
        /// 判断是否为功能键
        /// </summary>
        private static bool IsFunctionKey(Key key)
        {
            return key == Key.F1 || key == Key.F2 || key == Key.F3 || key == Key.F4 ||
                   key == Key.F5 || key == Key.F6 || key == Key.F7 || key == Key.F8 ||
                   key == Key.F9 || key == Key.F10 || key == Key.F11 || key == Key.F12 ||
                   key == Key.Escape || key == Key.PrintScreen || key == Key.Scroll ||
                   key == Key.Pause || key == Key.Insert || key == Key.Delete ||
                   key == Key.Help || key == Key.LWin || key == Key.RWin || key == Key.Apps;
        }

        /// <summary>
        /// 判断是否为可输入字符的键
        /// </summary>
        private static bool IsTypableKey(Key key)
        {
            // 字母
            if (key >= Key.A && key <= Key.Z)
                return true;

            // 数字
            if (key >= Key.D0 && key <= Key.D9)
                return true;

            // 数字小键盘
            if (key >= Key.NumPad0 && key <= Key.NumPad9)
                return true;

            // 标点符号
            return key == Key.Space || key == Key.OemComma || key == Key.OemPeriod ||
                   key == Key.OemQuestion || key == Key.OemSemicolon || key == Key.OemQuotes ||
                   key == Key.OemPipe || key == Key.OemMinus || key == Key.OemPlus ||
                   key == Key.OemOpenBrackets || key == Key.OemCloseBrackets ||
                   key == Key.OemBackslash || key == Key.OemTilde || key == Key.Decimal ||
                   key == Key.Divide || key == Key.Multiply || key == Key.Subtract || key == Key.Add;
        }

        private void ListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // 获取鼠标位置
            Point mousePosition = Mouse.GetPosition(ListView);

            // 使用 HitTest 查找鼠标位置的元素
            DependencyObject? hitElement = ListView.InputHitTest(mousePosition) as DependencyObject;

            // 向上遍历可视化树，查找 ListViewItem
            ListViewItem? clickedItem = VisualTreeExtensions.FindAncestor<ListViewItem>(hitElement);

            if (clickedItem != null)
            {
                // 找到了 ListViewItem，获取其数据项
                var dataItem = clickedItem.DataContext;

                // 更新选中项
                if (dataItem != null && !Equals(ListView.SelectedItem, dataItem))
                {
                    ListView.SelectedItem = dataItem;
                }

                // 允许菜单显示
                e.Handled = false;
            }
            else
            {
                // 点击空白区域，阻止菜单显示
                e.Handled = true;
            }
        }
    }
}
