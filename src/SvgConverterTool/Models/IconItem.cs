using System.ComponentModel;
using System.IO;
using System.Windows.Media;

namespace SvgConverterTool.Models
{
    /// <summary>
    /// 图标数据模型，转换结果与搜索结果共用
    /// </summary>
    public class IconItem : INotifyPropertyChanged
    {
        private string _fileName = string.Empty;

        /// <summary>文件名（无扩展名）</summary>
        public string FileName
        {
            get => _fileName;
            set
            {
                if (_fileName == value) return;
                _fileName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileName)));
            }
        }

        /// <summary>原始SVG路径（转换时使用）</summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>XAML代码字符串，用于卡片点击复制</summary>
        public string XamlCode { get; set; } = string.Empty;

        /// <summary>用于预览的图片</summary>
        public ImageSource? PreviewImage { get; set; }

        /// <summary>XAML文件路径，不存在时为 null</summary>
        public string? XamlPath { get; set; }

        /// <summary>SVG文件路径，不存在时为 null</summary>
        public string? SvgPath { get; set; }

        /// <summary>PNG文件路径，不存在时为 null</summary>
        public string? PngPath { get; set; }

        /// <summary>ICO文件路径，不存在时为 null</summary>
        public string? IcoPath { get; set; }

        public bool HasXaml => File.Exists(XamlPath);

        public bool HasSvg => File.Exists(SvgPath);

        public bool HasPng => File.Exists(PngPath);

        public bool HasIco => File.Exists(IcoPath);

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
