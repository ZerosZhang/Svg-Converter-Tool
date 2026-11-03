using SvgConverterTool.Helpers;
using SvgConverterTool.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SvgConverterTool.Views
{
    /// <summary>
    /// 图标搜索视图
    /// </summary>
    public partial class SearchView : UserControl
    {
        private static readonly string[] Extensions = { ".xaml", ".svg", ".png", ".ico" };

        private readonly ObservableCollection<IconItem> _results = new();
        private readonly string _outputDirectory;

        public SearchView()
        {
            InitializeComponent();
            ItemsResults.ItemsSource = _results;
            CardGridLayout.Attach(ItemsResults);

            _outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }

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
        /// 按名称关键字搜索Output目录中的四种格式，关键字为空时显示全部
        /// </summary>
        private void RefreshResults()
        {
            _results.Clear();

            string keyword = TxtSearch.Text.Trim();

            var names = Extensions
                .SelectMany(ext => Directory.EnumerateFiles(_outputDirectory, $"*{ext}"))
                .Select(Path.GetFileNameWithoutExtension)
                .OfType<string>();

            if (keyword.Length > 0)
            {
                names = names.Where(name => name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            var orderedNames = names
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

            foreach (string name in orderedNames)
            {
                _results.Add(new IconItem
                {
                    FileName = name,
                    XamlPath = GetExistingPath(name, ".xaml"),
                    SvgPath = GetExistingPath(name, ".svg"),
                    PngPath = GetExistingPath(name, ".png"),
                    IcoPath = GetExistingPath(name, ".ico"),
                    PreviewImage = LoadPreview(name)
                });
            }

            if (keyword.Length == 0)
            {
                TxtStatus.Text = $"共 {_results.Count} 个图标";
            }
            else
            {
                TxtStatus.Text = _results.Count == 0
                    ? $"未找到包含「{keyword}」的图标"
                    : $"找到 {_results.Count} 个图标";
            }
        }

        /// <summary>
        /// 读取搜索结果预览图，优先 PNG，其次 ICO
        /// </summary>
        private ImageSource? LoadPreview(string name)
        {
            string pngPath = Path.Combine(_outputDirectory, name + ".png");
            string icoPath = Path.Combine(_outputDirectory, name + ".ico");

            return PreviewHelper.LoadPng(pngPath) ?? PreviewHelper.LoadIcoLargest(icoPath);
        }

        /// <summary>
        /// 返回文件存在时的完整路径，否则返回 null
        /// </summary>
        private string? GetExistingPath(string name, string extension)
        {
            string path = Path.Combine(_outputDirectory, name + extension);
            return File.Exists(path) ? path : null;
        }

        /// <summary>
        /// 卡片复制或重命名后的状态提示
        /// </summary>
        private void IconCard_Message(object? sender, IconMessageEventArgs e)
        {
            TxtStatus.Text = e.Text;
        }
    }
}
