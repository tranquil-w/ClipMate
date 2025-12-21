using ClipMate.Presentation.Clipboard;
using System.Windows;
using System.Windows.Controls;

namespace ClipMate.Infrastructure
{
    public class ClipboardItemTemplateSelector : DataTemplateSelector
    {
        public required DataTemplate PlainTextTemplate { get; set; }
        public required DataTemplate ImageTemplate { get; set; }
        public required DataTemplate FileTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is FileDropListClipboard)
                return FileTemplate;
            else if (item is TextClipboard)
                return PlainTextTemplate;
            else if (item is ImageClipboard)
                return ImageTemplate;

            return base.SelectTemplate(item, container);
        }
    }
}
