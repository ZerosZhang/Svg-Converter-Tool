using SvgConverterTool.Helpers;
using SvgConverterTool.Models;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SvgConverterTool.Views
{
    /// <summary>
    /// 图标卡片消息
    /// </summary>
    public class IconMessageEventArgs : EventArgs
    {
        public IconMessageEventArgs(string text)
        {
            Text = text;
        }

        /// <summary>状态栏文字</summary>
        public string Text { get; }
    }

    /// <summary>
    /// 图标卡片，转换页与搜索页共用
    /// </summary>
    public partial class IconCard : UserControl
    {
        private readonly string _outputDirectory;
        private bool _isRenaming;
        private bool _cancelRename;
        private bool _endingRename;
        private bool _ignoreLostFocus;

        /// <summary>复制或重命名后的状态提示</summary>
        public event EventHandler<IconMessageEventArgs>? Message;

        public IconCard()
        {
            InitializeComponent();
            _outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
        }

        /// <summary>
        /// 点击卡片复制XAML
        /// </summary>
        private void Card_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not IconItem item)
                return;

            string? xaml = item.XamlCode;
            if (string.IsNullOrEmpty(xaml) && item.HasXaml)
            {
                xaml = File.ReadAllText(item.XamlPath!);
            }

            if (string.IsNullOrEmpty(xaml))
                return;

            try
            {
                Clipboard.SetText(xaml);
                Message?.Invoke(this, new IconMessageEventArgs($"已复制 [{item.FileName}] 的 XAML 代码"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 按格式复制按钮点击事件
        /// </summary>
        private void BtnCopyFormat_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string path)
                return;

            try
            {
                ClipboardHelper.CopyFile(path);
                Message?.Invoke(this, new IconMessageEventArgs($"已复制 [{Path.GetFileName(path)}]"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            e.Handled = true;
        }

        /// <summary>
        /// 点击文件名进入重命名
        /// </summary>
        private void FileName_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (sender is not TextBlock textBlock || textBlock.Parent is not Panel parent)
                return;

            TextBox? editor = parent.Children.OfType<TextBox>().FirstOrDefault();
            if (editor == null)
                return;

            textBlock.Visibility = Visibility.Collapsed;
            _ignoreLostFocus = false;
            editor.Text = textBlock.Text;
            editor.Visibility = Visibility.Visible;
            editor.Focus();
            editor.SelectAll();
        }

        /// <summary>
        /// 重命名输入框按键：Enter 提交，Esc 取消
        /// </summary>
        private void RenameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox editor)
                return;

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                CommitRename(editor);
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                _cancelRename = true;
                EndRename(editor);
                _cancelRename = false;
            }
        }

        /// <summary>
        /// 输入框失去焦点时提交重命名（冲突对话框打开期间和 Esc 取消时跳过）
        /// </summary>
        private void RenameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_ignoreLostFocus || _isRenaming || _cancelRename || _endingRename || sender is not TextBox editor)
                return;

            CommitRename(editor);
        }

        /// <summary>
        /// 提交重命名，必要时处理重名冲突
        /// </summary>
        private void CommitRename(TextBox editor)
        {
            if (DataContext is not IconItem item)
            {
                EndRename(editor);
                return;
            }

            _isRenaming = true;
            RenameResult result = IconRenameHelper.TryCommit(
                _outputDirectory, item, editor.Text, Window.GetWindow(this));
            _isRenaming = false;

            if (result == RenameResult.Retry)
            {
                _ignoreLostFocus = false;
                editor.Focus();
                editor.SelectAll();
                return;
            }

            if (result != RenameResult.Renamed)
            {
                EndRename(editor);
                return;
            }

            EndRename(editor);
            editor.Text = item.FileName;
            Message?.Invoke(this, new IconMessageEventArgs($"已重命名为 [{item.FileName}]"));
        }

        /// <summary>
        /// 结束重命名，恢复显示文件名
        /// </summary>
        private void EndRename(TextBox editor)
        {
            if (_endingRename)
                return;

            _endingRename = true;
            _ignoreLostFocus = true;
            editor.Visibility = Visibility.Collapsed;

            if (editor.Parent is Panel parent && parent.Children.OfType<TextBlock>().FirstOrDefault() is { } textBlock)
            {
                textBlock.Visibility = Visibility.Visible;
            }

            _endingRename = false;
        }
    }
}
