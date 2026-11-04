using SvgConverterTool.Models;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace SvgConverterTool.Helpers
{
    /// <summary>
    /// 图标分组工具：把图标的四种格式文件移动到目标分组目录
    /// </summary>
    public static class IconGroupHelper
    {
        /// <summary>
        /// 规范化分组名：统一分隔符并去掉首尾斜杠
        /// </summary>
        public static string NormalizeGroup(string input)
        {
            return input.Trim().Replace('\\', '/').Trim('/');
        }

        /// <summary>
        /// 校验分组名，空字符串表示默认分组（Output 根目录）
        /// </summary>
        public static bool IsValidGroup(string group, out string error)
        {
            if (group.Length == 0)
            {
                error = string.Empty;
                return true;
            }

            foreach (string segment in group.Split('/'))
            {
                if (segment.Length == 0 || segment == "." || segment == "..")
                {
                    error = "分组名不能包含空段、. 或 ..";
                    return false;
                }

                if (segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    error = "分组名不能包含以下字符：" + string.Join(" ", Path.GetInvalidFileNameChars());
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        /// <summary>
        /// 把图标移动到指定分组，成功后同步模型、路径与描述键
        /// </summary>
        /// <param name="outputDirectory">Output目录</param>
        /// <param name="item">要移动的图标</param>
        /// <param name="inputGroup">用户输入的分组名，空表示默认分组</param>
        /// <param name="owner">对话框宿主窗口</param>
        public static bool TryMove(string outputDirectory, IconItem item, string inputGroup, Window? owner)
        {
            string group = NormalizeGroup(inputGroup);

            if (string.Equals(group, item.Group, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!IsValidGroup(group, out string error))
            {
                MessageBox.Show(error, "无效的分组名", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            try
            {
                string targetFolder = group.Length == 0
                    ? outputDirectory
                    : Path.Combine(outputDirectory, group.Replace('/', Path.DirectorySeparatorChar));

                if (string.Equals(
                        Path.GetFullPath(targetFolder).TrimEnd(Path.DirectorySeparatorChar),
                        Path.GetFullPath(item.FolderPath).TrimEnd(Path.DirectorySeparatorChar),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                bool overwrite = false;

                if (HasConflict(item, targetFolder))
                {
                    MessageBoxResult result = MessageBox.Show(
                        $"「{item.FileName}」在目标分组中已存在同名文件，是否覆盖？",
                        "分组冲突", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

                    if (result != MessageBoxResult.Yes)
                        return false;

                    overwrite = true;
                }

                Directory.CreateDirectory(targetFolder);
                MoveFiles(item, targetFolder, overwrite);

                string oldKey = item.Key;
                item.Group = group;
                item.FolderPath = targetFolder;
                item.XamlPath = ExistingPath(targetFolder, item.FileName, ".xaml");
                item.SvgPath = ExistingPath(targetFolder, item.FileName, ".svg");
                item.PngPath = ExistingPath(targetFolder, item.FileName, ".png");
                item.IcoPath = ExistingPath(targetFolder, item.FileName, ".ico");
                item.Key = group.Length == 0 ? item.FileName : $"{group}/{item.FileName}";

                if (item.XamlPath != null)
                {
                    item.XamlCode = File.ReadAllText(item.XamlPath);
                }

                IconDescriptionStore.Rename(oldKey, item.Key);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移动分组失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 目标目录里是否已有同名文件
        /// </summary>
        private static bool HasConflict(IconItem item, string targetFolder)
        {
            return IconRenameHelper.Extensions.Any(extension =>
                File.Exists(Path.Combine(item.FolderPath, item.FileName + extension))
                && File.Exists(Path.Combine(targetFolder, item.FileName + extension)));
        }

        /// <summary>
        /// 把存在的格式文件平移到目标目录
        /// </summary>
        private static void MoveFiles(IconItem item, string targetFolder, bool overwrite)
        {
            foreach (string extension in IconRenameHelper.Extensions)
            {
                string source = Path.Combine(item.FolderPath, item.FileName + extension);
                if (!File.Exists(source))
                    continue;

                string destination = Path.Combine(targetFolder, item.FileName + extension);
                if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
                    continue;

                File.Move(source, destination, overwrite);
            }
        }

        /// <summary>
        /// 返回存在文件的完整路径，否则返回 null
        /// </summary>
        private static string? ExistingPath(string folder, string name, string extension)
        {
            string path = Path.Combine(folder, name + extension);
            return File.Exists(path) ? path : null;
        }
    }
}
