using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

namespace SvgConverterTool.Views
{
    /// <summary>
    /// ICO生成视图
    /// </summary>
    public partial class IcoConvertView : UserControl
    {
        private readonly ObservableCollection<IcoResult> _results = new();
        private readonly string _outputDirectory;

        public IcoConvertView()
        {
            InitializeComponent();
            ItemsResults.ItemsSource = _results;
            Loaded += (_, _) => UpdateGridColumns();
            ItemsResults.SizeChanged += (_, _) => UpdateGridColumns();

            _outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }
        }

        /// <summary>
        /// 选择SVG文件并生成ICO
        /// </summary>
        private async void BtnSelectFiles_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择 SVG 文件",
                Filter = "SVG 文件 (*.svg)|*.svg|所有文件 (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() != true)
                return;

            _results.Clear();
            BtnSelectFiles.IsEnabled = false;
            ProgressContainer.Visibility = Visibility.Visible;
            ProgressBar.Maximum = dialog.FileNames.Length;
            ProgressBar.Value = 0;
            TxtProgress.Text = $"0 / {dialog.FileNames.Length}";
            TxtStatus.Text = $"正在生成 {dialog.FileNames.Length} 个 ICO...";

            int successCount = 0;
            int failCount = 0;

            var progress = new Progress<int>(value =>
            {
                ProgressBar.Value = value;
                TxtProgress.Text = $"{value} / {dialog.FileNames.Length}";
            });

            await Task.Run(() =>
            {
                for (int i = 0; i < dialog.FileNames.Length; i++)
                {
                    string filePath = dialog.FileNames[i];
                    try
                    {
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        string icoOutputPath = Path.Combine(_outputDirectory, $"{fileName}.ico");
                        SvgConvertView.ConvertSvgToIco(filePath, icoOutputPath);

                        // 生成SVG矢量预览图（清晰锐利）
                        var preview = CreateSvgPreview(filePath);

                        Dispatcher.Invoke(() => _results.Add(new IcoResult
                        {
                            FileName = fileName,
                            IcoPath = icoOutputPath,
                            PreviewImage = preview
                        }));
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"转换失败 [{Path.GetFileName(filePath)}]:\n{ex.Message}", "错误",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }

                    ((IProgress<int>)progress).Report(i + 1);
                }
            });

            BtnSelectFiles.IsEnabled = true;
            ProgressContainer.Visibility = Visibility.Collapsed;
            TxtStatus.Text = $"生成完成：成功 {successCount} 个，失败 {failCount} 个。输出目录: {_outputDirectory}";
        }

        /// <summary>
        /// 从SVG源文件创建矢量预览图（无限清晰）
        /// </summary>
        private static ImageSource? CreateSvgPreview(string svgPath)
        {
            try
            {
                var settings = new WpfDrawingSettings
                {
                    IncludeRuntime = false,
                    TextAsGeometry = true,
                };
                var reader = new FileSvgReader(settings);
                var drawingGroup = reader.Read(svgPath);
                if (drawingGroup == null) return null;

                var drawingImage = new DrawingImage(drawingGroup);
                drawingImage.Freeze();
                return drawingImage;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 根据容器宽度动态计算UniformGrid列数
        /// </summary>
        private void UpdateGridColumns()
        {
            var grid = FindUniformGrid(ItemsResults);
            if (grid == null) return;
            double availableWidth = ItemsResults.ActualWidth;
            const double itemMinWidth = 172; // MinWidth 120 + Margin 16*2 + Padding
            int columns = Math.Max(1, (int)(availableWidth / itemMinWidth));
            grid.Columns = columns;
        }

        /// <summary>
        /// 在可视化树中查找UniformGrid
        /// </summary>
        private static UniformGrid? FindUniformGrid(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is UniformGrid grid) return grid;
                var result = FindUniformGrid(child);
                if (result != null) return result;
            }
            return null;
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
    }

    /// <summary>
    /// ICO生成结果
    /// </summary>
    public class IcoResult
    {
        public string FileName { get; set; } = string.Empty;
        public string IcoPath { get; set; } = string.Empty;
        public ImageSource? PreviewImage { get; set; }
    }
}
