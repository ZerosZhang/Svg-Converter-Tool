using System.Collections.Generic;
using System.Windows.Media;

namespace SvgConverterTool.Models
{
    /// <summary>
    /// 一组内容重复的图标
    /// </summary>
    public class DuplicateGroup
    {
        /// <summary>保留的文件名（无扩展名，创建时间最新）</summary>
        public string KeptName { get; set; } = string.Empty;

        /// <summary>保留图标的预览图</summary>
        public ImageSource? PreviewImage { get; set; }

        /// <summary>待删除的重复文件名</summary>
        public List<string> DuplicateNames { get; set; } = new();

        /// <summary>待删除文件名，用于界面展示</summary>
        public string DuplicateSummary => string.Join("、", DuplicateNames);

        /// <summary>待删除数量文字</summary>
        public string DuplicateCountText => $"删除 {DuplicateNames.Count} 个重复";
    }
}
