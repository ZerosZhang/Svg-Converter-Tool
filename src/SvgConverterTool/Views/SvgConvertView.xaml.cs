using Microsoft.Win32;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using SvgConverterTool.Helpers;
using SvgConverterTool.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace SvgConverterTool.Views
{
    /// <summary>
    /// SVG转换视图
    /// </summary>
    public partial class SvgConvertView : UserControl
    {
        private readonly ObservableCollection<IconItem> _results = new();
        private readonly string _outputDirectory;

        public SvgConvertView()
        {
            InitializeComponent();
            ItemsResults.ItemsSource = _results;
            CardGridLayout.Attach(ItemsResults);

            _outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }
        }

        /// <summary>
        /// 选择SVG文件按钮点击事件
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
            TxtStatus.Text = $"正在转换 {dialog.FileNames.Length} 个文件...";

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
                        var result = ConvertSingleFile(filePath);
                        if (result != null)
                        {
                            Dispatcher.Invoke(() => _results.Add(result));
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                        }
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
            TxtStatus.Text = $"转换完成：成功 {successCount} 个，失败 {failCount} 个。可在「管理」页打开输出目录";
        }

        /// <summary>
        /// 转换单个SVG文件
        /// </summary>
        private IconItem? ConvertSingleFile(string svgPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(svgPath);
            string xamlOutputPath = Path.Combine(_outputDirectory, $"{fileName}.xaml");
            string pngOutputPath = Path.Combine(_outputDirectory, $"{fileName}.png");

            // 转换配置
            var settings = new WpfDrawingSettings
            {
                IncludeRuntime = false,
                TextAsGeometry = true,
            };

            // ========== 1. 生成原始XAML（临时文件） ==========
            string tempXamlPath = Path.GetTempFileName() + ".xaml";
            try
            {
                var fileConverter = new FileSvgConverter(settings);
                fileConverter.Convert(svgPath, tempXamlPath);

                // ========== 2. 处理XAML格式 ==========
                string finalXaml = ProcessXamlFormat(tempXamlPath, fileName);
                File.WriteAllText(xamlOutputPath, finalXaml);

                // ========== 3. 生成PNG ==========
                var imageConverter = new ImageSvgConverter(settings);
                imageConverter.Convert(svgPath, pngOutputPath);

                // ========== 4. 复制原始SVG ==========
                string svgOutputPath = Path.Combine(_outputDirectory, $"{fileName}.svg");
                File.Copy(svgPath, svgOutputPath, overwrite: true);

                // ========== 5. 生成ICO ==========
                string icoOutputPath = Path.Combine(_outputDirectory, $"{fileName}.ico");
                ConvertSvgToIco(svgPath, icoOutputPath);

                // ========== 6. 生成预览图（DrawingImage） ==========
                var reader = new FileSvgReader(settings);
                DrawingGroup? drawingGroup = reader.Read(svgPath);
                if (drawingGroup == null)
                    return null;

                var drawingImage = new DrawingImage(drawingGroup);
                drawingImage.Freeze(); // 冻结以提高性能并允许跨线程使用

                return new IconItem
                {
                    FileName = fileName,
                    SourcePath = svgPath,
                    XamlCode = finalXaml,
                    PreviewImage = drawingImage,
                    XamlPath = xamlOutputPath,
                    PngPath = pngOutputPath,
                    SvgPath = svgOutputPath,
                    IcoPath = icoOutputPath
                };
            }
            finally
            {
                // 清理临时文件
                try
                {
                    if (File.Exists(tempXamlPath))
                        File.Delete(tempXamlPath);
                }
                catch { /* 忽略清理错误 */ }
            }
        }

        /// <summary>
        /// 将SVG转换为多尺寸ICO文件
        /// </summary>
        public static void ConvertSvgToIco(string svgPath, string icoOutputPath)
        {
            var sizes = new[] { 16, 32, 48, 64, 128, 256 };
            var entries = new List<(int Size, byte[] Png)>();

            var settings = new WpfDrawingSettings
            {
                IncludeRuntime = false,
                TextAsGeometry = true,
            };

            foreach (var size in sizes)
            {
                var reader = new FileSvgReader(settings);
                DrawingGroup? drawingGroup = reader.Read(svgPath);
                if (drawingGroup == null)
                    continue;

                var drawingImage = new DrawingImage(drawingGroup);
                drawingImage.Freeze();

                var visual = new DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    dc.DrawImage(drawingImage, new Rect(0, 0, size, size));
                }

                var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(visual);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using var ms = new MemoryStream();
                encoder.Save(ms);
                entries.Add((size, ms.ToArray()));
            }

            if (entries.Count == 0)
                return;

            using var fs = File.Create(icoOutputPath);
            using var bw = new BinaryWriter(fs);

            // ICONDIR
            bw.Write((ushort)0);
            bw.Write((ushort)1);
            bw.Write((ushort)entries.Count);

            int offset = 6 + entries.Count * 16;
            foreach (var (size, png) in entries)
            {
                bw.Write(size >= 256 ? (byte)0 : (byte)size);
                bw.Write(size >= 256 ? (byte)0 : (byte)size);
                bw.Write((byte)0);
                bw.Write((byte)0);
                bw.Write((ushort)1);
                bw.Write((ushort)32);
                bw.Write(png.Length);
                bw.Write(offset);
                offset += png.Length;
            }

            foreach (var (_, png) in entries)
                bw.Write(png);
        }

        /// <summary>
        /// 处理XAML格式：去掉双层DrawingGroup、去掉Name和xmlns、强制裁剪到1024画布、包装成纯净的DrawingImage
        /// </summary>
        private string ProcessXamlFormat(string tempXamlPath, string fileName)
        {
            string rawXaml = File.ReadAllText(tempXamlPath);

            XDocument doc;
            try
            {
                doc = XDocument.Parse(rawXaml);
            }
            catch
            {
                return rawXaml;
            }

            XNamespace ns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
            XNamespace xNs = "http://schemas.microsoft.com/winfx/2006/xaml";

            // 找到内层的 DrawingGroup（x:Name="DrawingLayer"）
            var targetDrawingGroup = doc.Descendants(ns + "DrawingGroup")
                .FirstOrDefault(e => e.Attribute(xNs + "Name")?.Value == "DrawingLayer");

            if (targetDrawingGroup == null)
            {
                targetDrawingGroup = doc.Root?.Elements(ns + "DrawingGroup").FirstOrDefault();
            }

            if (targetDrawingGroup == null)
            {
                targetDrawingGroup = doc.Root;
            }

            if (targetDrawingGroup == null)
                return rawXaml;

            // 去掉 x:Name 属性
            targetDrawingGroup.Attribute(xNs + "Name")?.Remove();

            // 统一为 1024 画布：去掉缩放变换，改用固定裁剪区域
            targetDrawingGroup.Element(ns + "DrawingGroup.Transform")?.Remove();
            targetDrawingGroup.Element(ns + "DrawingGroup.ClipGeometry")?.Remove();
            targetDrawingGroup.AddFirst(
                new XElement(ns + "DrawingGroup.ClipGeometry",
                    new XElement(ns + "RectangleGeometry", new XAttribute("Rect", "0,0,1024,1024"))));

            // 获取 DrawingGroup 的XML字符串
            string dgXml = targetDrawingGroup.ToString(SaveOptions.None);

            // 去掉 DrawingGroup 上的 xmlns 和 xmlns:x 声明（字符串替换方式）
            dgXml = dgXml.Replace(" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"", "");
            dgXml = dgXml.Replace(" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"", "");

            // 构造纯净的 DrawingImage XAML（不带任何 xmlns 和 x:Key）
            string result = $"<!--{fileName}图标-->\n" +
                            $"<DrawingImage>\n" +
                            $"    <DrawingImage.Drawing>\n" +
                            $"        {dgXml}\n" +
                            $"    </DrawingImage.Drawing>\n" +
                            $"</DrawingImage>";

            return result;
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
