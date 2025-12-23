using Serilog;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;
using ClipMate.Infrastructure;
using HandyControl.Controls;

namespace ClipMate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : HandyControl.Controls.Window
    {
        private readonly NoActivateWindowController _noActivateWindowController;

        /// <summary>
        /// 获取无焦点窗口控制器，用于临时激活窗口
        /// </summary>
        internal NoActivateWindowController NoActivateWindowController => _noActivateWindowController;

        public MainWindow()
        {
            InitializeComponent();

            Closing += MainWindow_Closing;

            _noActivateWindowController = new NoActivateWindowController(this);
            ContextMenuTracker.ContextMenuStateChanged += OnContextMenuStateChanged;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // 获取当前窗口句柄
            var hwnd = new WindowInteropHelper(this).Handle;

            // 移除系统菜单，防止Alt键触发系统菜单
            Interop.WindowStyle.RemoveSystemMenu(hwnd);

            _noActivateWindowController.Attach();
        }

        /// <summary>
        /// 在 InputBindings 之前处理 Escape 键，确保搜索框有内容时优先清除内容
        /// </summary>
        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
            {
                return;
            }

            // 查找搜索框
            SearchBar? searchBox = VisualTreeExtensions.FindDescendant<SearchBar>(this);
            if (searchBox == null)
            {
                return;
            }

            // 如果搜索框有内容，清除内容并阻止事件继续传播
            if (!string.IsNullOrEmpty(searchBox.Text))
            {
                searchBox.Text = string.Empty;
                e.Handled = true;
            }
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            _noActivateWindowController.ResumeNoActivate();

            // 清除键盘焦点，防止搜索框在窗口重新打开时保持选中样式
            FocusManager.SetFocusedElement(this, null);
            Keyboard.ClearFocus();

            // Prevent the window from closing
            Hide();
            e.Cancel = true;
        }

        private void OnContextMenuStateChanged(bool isOpen)
        {
            _noActivateWindowController.SetOutsideClickSuppressed(isOpen);
        }
    }
}
