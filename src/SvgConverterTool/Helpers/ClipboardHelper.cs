using System;
using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SvgConverterTool.Helpers
{
    /// <summary>
    /// 剪贴板复制工具，按文件格式选择复制方式
    /// </summary>
    public static class ClipboardHelper
    {
        /// <summary>
        /// 复制文件：XAML 复制文本内容，PNG 复制图片，其余格式复制文件本身
        /// </summary>
        public static void CopyFile(string path)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".xaml":
                    Clipboard.SetText(File.ReadAllText(path));
                    break;
                case ".png":
                    CopyPng(path);
                    break;
                default:
                    Clipboard.SetFileDropList(new StringCollection { path });
                    break;
            }
        }

        /// <summary>
        /// 将PNG文件以位图形式复制到剪贴板
        /// </summary>
        private static void CopyPng(string path)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            Clipboard.SetImage(bitmap);
        }
    }
}
