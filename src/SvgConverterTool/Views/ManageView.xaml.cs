using SvgConverterTool.Helpers;
using SvgConverterTool.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SvgConverterTool.Views
{
    /// <summary>
    /// 管理视图
    /// </summary>
    public partial class ManageView : UserControl
    {
        private static readonly string[] Extensions = { ".xaml", ".svg", ".png", ".ico" };

        private readonly ObservableCollection<DuplicateGroup> _groups = new();
        private readonly string _outputDirectory;

        public ManageView()
        {
            InitializeComponent();
            ItemsGroups.ItemsSource = _groups;

            _outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }

            TxtStatus.Text = "点击「扫描重复数据」开始检查";
        }

        /// <summary>
        /// 打开输出目录
        /// </summary>
        private void BtnOpenOutput_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(_outputDirectory))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _outputDirectory,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show("输出目录不存在。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 使用默认浏览器打开阿里巴巴矢量图标库
        /// </summary>
        private void BtnOpenIconFont_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.iconfont.cn/",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开网址失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 扫描重复数据（按 SVG 内容判定）
        /// </summary>
        private async void BtnScan_Click(object sender, RoutedEventArgs e)
        {
            BtnScan.IsEnabled = false;
            BtnDelete.IsEnabled = false;
            _groups.Clear();
            TxtStatus.Text = "正在扫描重复数据...";

            List<DuplicateGroup> groups = await Task.Run(ScanDuplicates);

            foreach (DuplicateGroup group in groups)
            {
                _groups.Add(group);
            }

            int removable = groups.Sum(group => group.DuplicateNames.Count);
            TxtStatus.Text = groups.Count == 0
                ? "未发现重复数据"
                : $"发现 {groups.Count} 组重复，可删除 {removable} 个图标";

            BtnDelete.IsEnabled = groups.Count > 0;
            BtnScan.IsEnabled = true;
        }

        /// <summary>
        /// 删除所有重复项，每组保留最新一份
        /// </summary>
        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_groups.Count == 0)
                return;

            int removable = _groups.Sum(group => group.DuplicateNames.Count);
            MessageBoxResult confirm = MessageBox.Show(
                $"将永久删除 {removable} 个重复图标，每组保留最新一份。\n此操作不可恢复，是否继续？",
                "删除重复数据", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            int deleted = 0;
            int failed = 0;

            foreach (DuplicateGroup group in _groups)
            {
                foreach (string name in group.DuplicateNames)
                {
                    foreach (string extension in Extensions)
                    {
                        string path = Path.Combine(_outputDirectory, name + extension);
                        if (!File.Exists(path))
                            continue;

                        try
                        {
                            File.Delete(path);
                            deleted++;
                        }
                        catch
                        {
                            failed++;
                        }
                    }
                }
            }

            int kept = _groups.Count;
            _groups.Clear();
            BtnDelete.IsEnabled = false;
            TxtStatus.Text = failed == 0
                ? $"已删除 {deleted} 个文件，保留 {kept} 个图标"
                : $"已删除 {deleted} 个文件，{failed} 个删除失败，保留 {kept} 个图标";
        }

        /// <summary>
        /// 扫描Output目录，按SVG内容分组并保留创建时间最新的一份
        /// </summary>
        private List<DuplicateGroup> ScanDuplicates()
        {
            var namesByHash = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (string svgPath in Directory.EnumerateFiles(_outputDirectory, "*.svg"))
            {
                try
                {
                    using FileStream stream = File.OpenRead(svgPath);
                    string hash = Convert.ToHexString(SHA256.HashData(stream));
                    string name = Path.GetFileNameWithoutExtension(svgPath);

                    if (!namesByHash.TryGetValue(hash, out List<string>? names))
                    {
                        names = new List<string>();
                        namesByHash[hash] = names;
                    }

                    names.Add(name);
                }
                catch
                {
                    // 读取失败的文件跳过
                }
            }

            var groups = new List<DuplicateGroup>();

            foreach (List<string> names in namesByHash.Values)
            {
                if (names.Count < 2)
                    continue;

                List<string> ordered = names
                    .OrderByDescending(GetNewestTime)
                    .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                groups.Add(new DuplicateGroup
                {
                    KeptName = ordered[0],
                    PreviewImage = PreviewHelper.LoadPng(Path.Combine(_outputDirectory, ordered[0] + ".png")),
                    DuplicateNames = ordered.Skip(1).ToList()
                });
            }

            return groups
                .OrderBy(group => group.KeptName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// 取该图标四种格式文件中最新的创建时间
        /// </summary>
        private DateTime GetNewestTime(string name)
        {
            DateTime newest = DateTime.MinValue;

            foreach (string extension in Extensions)
            {
                string path = Path.Combine(_outputDirectory, name + extension);
                if (!File.Exists(path))
                    continue;

                DateTime created = File.GetCreationTimeUtc(path);
                if (created > newest)
                    newest = created;
            }

            return newest;
        }
    }
}
