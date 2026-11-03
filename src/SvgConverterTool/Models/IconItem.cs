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
        private ImageSource? _previewImage;
        private string _description = string.Empty;

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

        /// <summary>所属分组，取 Output 下的相对目录名，根目录为空字符串</summary>
        public string Group { get; set; } = string.Empty;

        /// <summary>备注唯一键（相对路径/图标名，正斜杠分隔）</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>图标所在目录，空字符串表示 Output 根目录</summary>
        public string FolderPath { get; set; } = string.Empty;

        /// <summary>备注：这个图标用在什么场景</summary>
        public string Description
        {
            get => _description;
            set
            {
                if (_description == value) return;
                _description = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
            }
        }

        /// <summary>XAML代码字符串，用于卡片点击复制</summary>
        public string XamlCode { get; set; } = string.Empty;

        /// <summary>用于预览的图片，异步加载完成后更新</summary>
        public ImageSource? PreviewImage
        {
            get => _previewImage;
            set
            {
                if (ReferenceEquals(_previewImage, value)) return;
                _previewImage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PreviewImage)));
            }
        }

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
