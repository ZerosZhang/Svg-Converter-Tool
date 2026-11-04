using SvgConverterTool.Helpers;
using SvgConverterTool.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SvgConverterTool.Views
{
    /// <summary>
    /// 图标搜索视图：一行一个图标，含名称、分组、格式与描述
    /// </summary>
    public partial class SearchView : UserControl
    {
        private static readonly string[] Extensions = { ".xaml", ".svg", ".png", ".ico" };

        /// <summary>描述列之外各列宽度之和，用于自适应描述列</summary>
        private const double FixedColumnWidth = 84 + 200 + 150;

        private const string AllGroupsLabel = "全部分组";
        private const string DefaultGroupLabel = "默认分组";

        private readonly ObservableCollection<IconItem> _results = new();
        private readonly ObservableCollection<GroupOption> _groups = new();
        private readonly string _outputDirectory;
        private List<string> _groupSource = new();
        private bool _groupsInitialized;
        private CancellationTokenSource? _previewCts;
        private bool _updatingGroups;
        private bool _isRenaming;
        private bool _cancelRename;
        private bool _endingRename;
        private bool _ignoreLostFocus;
        private bool _isMovingGroup;
        private bool _cancelGroupEdit;
        private bool _endingGroupEdit;
        private bool _ignoreGroupLostFocus;

        /// <summary>行内分组编辑框的可选项，第一项为默认分组</summary>
        public ObservableCollection<string> GroupChoices { get; } = new();

        public SearchView()
        {
            InitializeComponent();
            ListResults.ItemsSource = _results;
            CmbGroup.ItemsSource = _groups;

            _outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }

            Unloaded += (_, _) => _previewCts?.Cancel();
            RefreshResults();
        }

        /// <summary>
        /// 搜索框内容变化时刷新结果
        /// </summary>
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshResults();
        }

        /// <summary>
        /// 分组筛选变化时刷新结果（重建下拉项期间跳过）
        /// </summary>
        private void CmbGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_updatingGroups)
                return;

            RefreshResults();
        }

        /// <summary>
        /// 扫描 Output 目录（含子目录），按分组和关键字过滤后展示
        /// </summary>
        private void RefreshResults()
        {
            _previewCts?.Cancel();
            var cts = new CancellationTokenSource();
            _previewCts = cts;

            _results.Clear();

            string keyword = TxtSearch.Text.Trim();
            string? group = SelectedGroup();
            List<IconItem> all = ScanIcons();
            UpdateGroupOptions(all);

            var items = all
                .Where(item => group == null || string.Equals(item.Group, group, StringComparison.OrdinalIgnoreCase))
                .Where(item => Matches(item, keyword))
                .ToList();

            foreach (IconItem item in items)
            {
                _results.Add(item);
            }

            _ = LoadPreviewsAsync(items, cts.Token);

            TxtStatus.Text = keyword.Length == 0 && group == null
                ? $"共 {_results.Count} 个图标"
                : _results.Count == 0
                    ? "未找到匹配的图标"
                    : $"找到 {_results.Count} 个图标";
        }

        /// <summary>
        /// 名称、分组或描述命中关键字
        /// </summary>
        private static bool Matches(IconItem item, string keyword)
        {
            return keyword.Length == 0
                || item.FileName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || item.Group.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || item.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 当前选中的分组，null 表示不筛选
        /// </summary>
        private string? SelectedGroup()
        {
            return (CmbGroup.SelectedItem as GroupOption)?.Value;
        }

        /// <summary>
        /// 按当前图标集重建分组下拉项，保留原选择
        /// </summary>
        private void UpdateGroupOptions(List<IconItem> all)
        {
            List<string> groups = all
                .Select(item => item.Group)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group, StringComparer.OrdinalIgnoreCase)
                .ToList();

            // 分组集合没变化就不重建，避免把正在展开的下拉框冲掉
            if (_groupsInitialized && _groupSource.SequenceEqual(groups, StringComparer.OrdinalIgnoreCase))
                return;

            string? current = SelectedGroup();
            _groupSource = groups;
            _groupsInitialized = true;

            _updatingGroups = true;

            _groups.Clear();
            _groups.Add(new GroupOption(null, AllGroupsLabel));

            GroupChoices.Clear();
            GroupChoices.Add(DefaultGroupLabel);

            foreach (string group in groups)
            {
                _groups.Add(new GroupOption(group, group.Length == 0 ? DefaultGroupLabel : group));

                if (group.Length > 0)
                {
                    GroupChoices.Add(group);
                }
            }

            CmbGroup.SelectedItem = _groups.FirstOrDefault(option =>
                string.Equals(option.Value, current, StringComparison.OrdinalIgnoreCase)) ?? _groups[0];

            _updatingGroups = false;
        }

        /// <summary>
        /// 递归扫描 Output 目录，按「相对路径/图标名」合并四种格式
        /// </summary>
        private List<IconItem> ScanIcons()
        {
            var byKey = new Dictionary<string, IconItem>(StringComparer.OrdinalIgnoreCase);

            foreach (string path in Directory.EnumerateFiles(_outputDirectory, "*", SearchOption.AllDirectories))
            {
                string extension = Path.GetExtension(path);
                if (!Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                    continue;

                string relative = Path.GetRelativePath(_outputDirectory, path);
                string key = relative[..^extension.Length].Replace('\\', '/');

                if (!byKey.TryGetValue(key, out IconItem? item))
                {
                    string folder = Path.GetDirectoryName(relative) ?? string.Empty;

                    item = new IconItem
                    {
                        FileName = Path.GetFileNameWithoutExtension(path),
                        Key = key,
                        Group = folder.Replace('\\', '/'),
                        FolderPath = Path.GetDirectoryName(path) ?? _outputDirectory,
                        Description = IconDescriptionStore.Get(key)
                    };

                    byKey[key] = item;
                }

                switch (extension.ToLowerInvariant())
                {
                    case ".xaml": item.XamlPath = path; break;
                    case ".svg": item.SvgPath = path; break;
                    case ".png": item.PngPath = path; break;
                    case ".ico": item.IcoPath = path; break;
                }
            }

            return byKey.Values
                .OrderBy(item => item.Group, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.FileName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// 后台逐张解码预览图并回填到对应行，新的搜索会取消上一轮
        /// </summary>
        private async Task LoadPreviewsAsync(List<IconItem> items, CancellationToken token)
        {
            try
            {
                foreach (IconItem item in items)
                {
                    token.ThrowIfCancellationRequested();
                    ImageSource? image = await Task.Run(() => LoadPreview(item), token);
                    item.PreviewImage = image;
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// 读取预览图，优先 PNG，其次 ICO
        /// </summary>
        private static ImageSource? LoadPreview(IconItem item)
        {
            ImageSource? image = item.PngPath != null ? PreviewHelper.LoadPng(item.PngPath) : null;
            return image ?? (item.IcoPath != null ? PreviewHelper.LoadIcoLargest(item.IcoPath) : null);
        }

        /// <summary>
        /// 点击名称进入重命名
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
        /// 点击分组标签进入分组编辑
        /// </summary>
        private void GroupTag_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (sender is not Border tag || tag.DataContext is not IconItem item || tag.Parent is not Panel parent)
                return;

            ComboBox? editor = parent.Children.OfType<ComboBox>().FirstOrDefault();
            if (editor == null)
                return;

            tag.Visibility = Visibility.Collapsed;
            _ignoreGroupLostFocus = false;
            editor.IsDropDownOpen = false;
            editor.Text = item.Group.Length == 0 ? DefaultGroupLabel : item.Group;
            editor.Visibility = Visibility.Visible;
            editor.Focus();
        }

        /// <summary>
        /// 分组编辑框按键：Enter 提交，Esc 取消
        /// </summary>
        private void GroupBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not ComboBox editor)
                return;

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                CommitGroup(editor);
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                _cancelGroupEdit = true;
                EndGroupEdit(editor);
                _cancelGroupEdit = false;
            }
        }

        /// <summary>
        /// 分组编辑框失去焦点时提交（下拉展开、移动过程中和 Esc 取消时跳过）
        /// </summary>
        private void GroupBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_ignoreGroupLostFocus || _isMovingGroup || _cancelGroupEdit || _endingGroupEdit
                || sender is not ComboBox editor)
            {
                return;
            }

            CommitGroup(editor);
        }

        /// <summary>
        /// 提交分组修改，把图标文件移动到目标分组目录
        /// </summary>
        private void CommitGroup(ComboBox editor)
        {
            if (editor.DataContext is not IconItem item)
            {
                EndGroupEdit(editor);
                return;
            }

            string input = editor.Text.Trim();
            if (input == DefaultGroupLabel)
                input = string.Empty;

            if (string.Equals(IconGroupHelper.NormalizeGroup(input), item.Group, StringComparison.OrdinalIgnoreCase))
            {
                EndGroupEdit(editor);
                return;
            }

            _isMovingGroup = true;
            bool moved = IconGroupHelper.TryMove(_outputDirectory, item, input, Window.GetWindow(this));
            _isMovingGroup = false;

            EndGroupEdit(editor);

            if (!moved)
                return;

            RefreshResults();

            TxtStatus.Text = item.Group.Length == 0
                ? $"已移动 [{item.FileName}] 到默认分组"
                : $"已移动 [{item.FileName}] 到 [{item.Group}]";
        }

        /// <summary>
        /// 结束分组编辑，恢复显示分组标签
        /// </summary>
        private void EndGroupEdit(ComboBox editor)
        {
            if (_endingGroupEdit)
                return;

            _endingGroupEdit = true;
            _ignoreGroupLostFocus = true;
            editor.IsDropDownOpen = false;
            editor.Visibility = Visibility.Collapsed;

            if (editor.Parent is Panel parent && parent.Children.OfType<Border>().FirstOrDefault() is { } tag)
            {
                tag.Visibility = Visibility.Visible;
            }

            _endingGroupEdit = false;
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
        /// 提交重命名，成功后同步迁移描述键
        /// </summary>
        private void CommitRename(TextBox editor)
        {
            if (editor.DataContext is not IconItem item)
            {
                EndRename(editor);
                return;
            }

            string oldKey = item.Key;
            _isRenaming = true;
            RenameResult result = IconRenameHelper.TryCommit(
                item.FolderPath, item, editor.Text, Window.GetWindow(this));
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

            string newKey = string.IsNullOrEmpty(item.Group) ? item.FileName : $"{item.Group}/{item.FileName}";
            IconDescriptionStore.Rename(oldKey, newKey);
            item.Key = newKey;

            EndRename(editor);
            editor.Text = item.FileName;
            TxtStatus.Text = $"已重命名为 [{item.FileName}]";
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

        /// <summary>
        /// 描述失焦时写入 descriptions.json
        /// </summary>
        private void DescriptionBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox box && box.DataContext is IconItem item)
            {
                IconDescriptionStore.Set(item.Key, item.Description);
            }
        }

        /// <summary>
        /// 描述输入框回车结束编辑
        /// </summary>
        private void DescriptionBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || sender is not TextBox box)
                return;

            e.Handled = true;

            if (box.DataContext is IconItem item)
            {
                IconDescriptionStore.Set(item.Key, item.Description);
            }

            Keyboard.ClearFocus();
        }

        /// <summary>
        /// 按格式复制对应文件
        /// </summary>
        private void BtnCopyFormat_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string path)
                return;

            try
            {
                ClipboardHelper.CopyFile(path);
                TxtStatus.Text = $"已复制 [{Path.GetFileName(path)}]";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"复制失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            e.Handled = true;
        }

        /// <summary>
        /// 分组/描述列占满剩余宽度
        /// </summary>
        private void ListResults_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DetailColumn.Width = Math.Max(260, e.NewSize.Width - FixedColumnWidth - 24);
        }

        /// <summary>
        /// 分组下拉项：Value 为 null 表示不筛选
        /// </summary>
        private sealed class GroupOption
        {
            public GroupOption(string? value, string label)
            {
                Value = value;
                Label = label;
            }

            public string? Value { get; }

            public string Label { get; }

            public override string ToString() => Label;
        }
    }
}
