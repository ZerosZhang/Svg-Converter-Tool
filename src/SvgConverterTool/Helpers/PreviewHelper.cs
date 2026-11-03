using System;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SvgConverterTool.Helpers
{
    /// <summary>
    /// 图标预览图加载工具
    /// </summary>
    public static class PreviewHelper
    {
        /// <summary>
        /// 读取PNG文件作为预览图，失败时返回 null
        /// </summary>
        public static ImageSource? LoadPng(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 读取ICO文件中尺寸最大的一帧作为预览图，失败时返回 null
        /// </summary>
        public static ImageSource? LoadIcoLargest(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                var decoder = new IconBitmapDecoder(new Uri(path),
                    BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                var frame = decoder.Frames.OrderByDescending(f => f.PixelWidth).FirstOrDefault();
                if (frame == null)
                    return null;

                frame.Freeze();
                return frame;
            }
            catch
            {
                return null;
            }
        }
    }
}
