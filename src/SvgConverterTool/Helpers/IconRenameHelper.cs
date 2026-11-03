using SvgConverterTool.Models;
using SvgConverterTool.Views;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace SvgConverterTool.Helpers
{
    /// <summary>
    /// 重命名结果
    /// </summary>
    public enum RenameResult
    {
        /// <summary>已重命名</summary>
        Renamed,
        /// <summary>取消或无变化</summary>
        Canceled,
        /// <summary>需要重新输入（冲突后选择重新命名）</summary>
        Retry
    }

    /// <summary>
    /// 图标文件重命名工具：处理校验、重名冲突与四种格式同步改名
    /// </summary>
    public static class IconRenameHelper
    {
        /// <summary>图标包含的四种扩展名</summary>
        public static readonly string[] Extensions = { ".xaml", ".svg", ".png", ".ico" };

        /// <summary>
        /// 按新名称重命名图标文件，必要时弹出冲突对话框
        /// </summary>
        /// <param name="outputDirectory">Output目录</param>
        /// <param name="item">要重命名的图标</param>
        /// <param name="inputName">用户输入的名称，可能带扩展名</param>
        /// <param name="owner">对话框宿主窗口</param>
        public static RenameResult TryCommit(string outputDirectory, IconItem item, string inputName, Window? owner)
        {
            string newName = Normalize(inputName);

            if (newName.Length == 0 || newName == item.FileName)
                return RenameResult.Canceled;

            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("文件名不能包含以下字符：" + string.Join(" ", Path.GetInvalidFileNameChars()),
                    "无效的文件名", MessageBoxButton.OK, MessageBoxImage.Warning);
                return RenameResult.Canceled;
            }

            try
            {
                bool hasConflict = Extensions.Any(ext =>
                    File.Exists(Path.Combine(outputDirectory, newName + ext)));

                if (hasConflict)
                {
                    var dialog = new RenameConflictDialog(
                        newName,
                        item.PreviewImage,
                        PreviewHelper.LoadPng(Path.Combine(outputDirectory, newName + ".png")))
                    {
                        Owner = owner
                    };
                    dialog.ShowDialog();

                    switch (dialog.Action)
                    {
                        case RenameConflictAction.Overwrite:
                            RenameFiles(outputDirectory, item.FileName, newName, overwrite: true);
                            ApplyPaths(outputDirectory, item, newName);
                            return RenameResult.Renamed;
                        case RenameConflictAction.AddSuffix:
                            newName = GetAvailableName(outputDirectory, newName);
                            RenameFiles(outputDirectory, item.FileName, newName, overwrite: false);
                            ApplyPaths(outputDirectory, item, newName);
                            return RenameResult.Renamed;
                        case RenameConflictAction.Retry:
                            return RenameResult.Retry;
                        default:
                            return RenameResult.Canceled;
                    }
                }

                RenameFiles(outputDirectory, item.FileName, newName, overwrite: false);
                ApplyPaths(outputDirectory, item, newName);
                return RenameResult.Renamed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重命名失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return RenameResult.Canceled;
            }
        }

        /// <summary>
        /// 去除输入名称中的图标扩展名
        /// </summary>
        private static string Normalize(string inputName)
        {
            string name = inputName.Trim();

            if (Extensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase))
                name = Path.GetFileNameWithoutExtension(name);

            return name;
        }

        /// <summary>
        /// 重命名四种格式文件，并同步 XAML 中的名称注释
        /// </summary>
        private static void RenameFiles(string outputDirectory, string oldName, string newName, bool overwrite)
        {
            foreach (string extension in Extensions)
            {
                string oldPath = Path.Combine(outputDirectory, oldName + extension);
                if (!File.Exists(oldPath))
                    continue;

                string newPath = Path.Combine(outputDirectory, newName + extension);
                if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                File.Move(oldPath, newPath, overwrite);
            }

            UpdateXamlComment(outputDirectory, newName);
        }

        /// <summary>
        /// 更新XAML文件首行的名称注释
        /// </summary>
        private static void UpdateXamlComment(string outputDirectory, string name)
        {
            string xamlPath = Path.Combine(outputDirectory, name + ".xaml");
            if (!File.Exists(xamlPath))
                return;

            string text = File.ReadAllText(xamlPath);
            if (!text.StartsWith("<!--", StringComparison.Ordinal))
                return;

            int commentEnd = text.IndexOf("-->", StringComparison.Ordinal);
            if (commentEnd > 0)
            {
                File.WriteAllText(xamlPath, $"<!--{name}图标-->" + text[(commentEnd + 3)..]);
            }
        }

        /// <summary>
        /// 重命名后同步模型的名称与文件路径
        /// </summary>
        private static void ApplyPaths(string outputDirectory, IconItem item, string newName)
        {
            item.XamlPath = ExistingPath(outputDirectory, newName, ".xaml");
            item.SvgPath = ExistingPath(outputDirectory, newName, ".svg");
            item.PngPath = ExistingPath(outputDirectory, newName, ".png");
            item.IcoPath = ExistingPath(outputDirectory, newName, ".ico");
            item.FileName = newName;

            if (item.XamlPath != null)
            {
                item.XamlCode = File.ReadAllText(item.XamlPath);
            }
        }

        /// <summary>
        /// 返回存在文件的完整路径，否则返回 null
        /// </summary>
        private static string? ExistingPath(string outputDirectory, string name, string extension)
        {
            string path = Path.Combine(outputDirectory, name + extension);
            return File.Exists(path) ? path : null;
        }

        /// <summary>
        /// 生成一个四种格式都不冲突的名称
        /// </summary>
        private static string GetAvailableName(string outputDirectory, string baseName)
        {
            int index = 1;

            while (true)
            {
                string candidate = $"{baseName}_{index}";
                bool exists = Extensions.Any(ext =>
                    File.Exists(Path.Combine(outputDirectory, candidate + ext)));

                if (!exists)
                    return candidate;

                index++;
            }
        }
    }
}
