using System.Windows;
using System.Windows.Media;

namespace SvgConverterTool.Views
{
    /// <summary>
    /// 重命名冲突时的处理方式
    /// </summary>
    public enum RenameConflictAction
    {
        Cancel,
        Overwrite,
        AddSuffix,
        Retry
    }

    /// <summary>
    /// 重命名冲突提示对话框，并排展示当前图标与已存在图标
    /// </summary>
    public partial class RenameConflictDialog : Window
    {
        /// <summary>用户选择的处理方式</summary>
        public RenameConflictAction Action { get; private set; } = RenameConflictAction.Cancel;

        public RenameConflictDialog(string name, ImageSource? currentPreview, ImageSource? existingPreview)
        {
            InitializeComponent();
            TxtMessage.Text = $"「{name}」已存在以下格式的文件，请选择处理方式：";

            ImgCurrent.Source = currentPreview;
            TxtNoCurrent.Visibility = currentPreview == null ? Visibility.Visible : Visibility.Collapsed;

            ImgExisting.Source = existingPreview;
            TxtNoExisting.Visibility = existingPreview == null ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Overwrite_Click(object sender, RoutedEventArgs e)
        {
            Action = RenameConflictAction.Overwrite;
            DialogResult = true;
        }

        private void AddSuffix_Click(object sender, RoutedEventArgs e)
        {
            Action = RenameConflictAction.AddSuffix;
            DialogResult = true;
        }

        private void Retry_Click(object sender, RoutedEventArgs e)
        {
            Action = RenameConflictAction.Retry;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Action = RenameConflictAction.Cancel;
            DialogResult = false;
        }
    }
}
