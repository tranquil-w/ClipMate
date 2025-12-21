using HandyControl.Controls;

namespace ClipMate.Windows
{
    /// <summary>
    /// HandyControl 风格的 Prism 对话框窗口
    /// 支持 HandyControl 主题自动切换
    /// </summary>
    public partial class HandyDialogWindow : Window, IDialogWindow
    {
        public HandyDialogWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Prism 对话框结果
        /// </summary>
        public IDialogResult? Result { get; set; }
    }
}
