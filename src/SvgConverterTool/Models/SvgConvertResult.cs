using System.Windows.Media;

namespace SvgConverterTool.Models
{
    /// <summary>
    /// SVG转换结果数据模型
    /// </summary>
    public class SvgConvertResult
    {
        /// <summary>文件名（无扩展名）</summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>原始SVG完整路径</summary>
        public string FullPath { get; set; } = string.Empty;

        /// <summary>生成的XAML代码字符串</summary>
        public string XamlCode { get; set; } = string.Empty;

        /// <summary>用于预览的DrawingImage对象</summary>
        public DrawingImage? PreviewImage { get; set; }

        /// <summary>生成的XAML文件路径</summary>
        public string XamlOutputPath { get; set; } = string.Empty;

        /// <summary>生成的PNG文件路径</summary>
        public string PngOutputPath { get; set; } = string.Empty;
    }
}
