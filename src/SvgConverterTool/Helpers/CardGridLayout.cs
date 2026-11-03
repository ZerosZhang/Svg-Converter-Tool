using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SvgConverterTool.Helpers
{
    /// <summary>
    /// 图标卡片网格布局：按容器宽度动态计算每行列数
    /// </summary>
    public static class CardGridLayout
    {
        /// <summary>单个卡片（含间距）的最小参考宽度</summary>
        private const double ItemMinWidth = 230;

        /// <summary>
        /// 绑定ItemsControl，在加载和尺寸变化时更新UniformGrid列数
        /// </summary>
        public static void Attach(ItemsControl itemsControl)
        {
            itemsControl.Loaded += (_, _) => UpdateColumns(itemsControl);
            itemsControl.SizeChanged += (_, _) => UpdateColumns(itemsControl);
        }

        /// <summary>
        /// 根据可用宽度计算列数，使用ScrollViewer宽度避免滚动条出现时列数跳变
        /// </summary>
        private static void UpdateColumns(ItemsControl itemsControl)
        {
            UniformGrid? grid = FindUniformGrid(itemsControl);
            if (grid == null)
                return;

            double availableWidth = FindAncestorScrollViewer(itemsControl)?.ActualWidth ?? itemsControl.ActualWidth;
            grid.Columns = Math.Max(1, (int)(availableWidth / ItemMinWidth));
        }

        /// <summary>
        /// 查找ItemsControl外层的ScrollViewer
        /// </summary>
        private static ScrollViewer? FindAncestorScrollViewer(DependencyObject current)
        {
            DependencyObject? parent = VisualTreeHelper.GetParent(current);

            while (parent != null)
            {
                if (parent is ScrollViewer scrollViewer)
                    return scrollViewer;

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        /// <summary>
        /// 在可视化树中查找UniformGrid
        /// </summary>
        private static UniformGrid? FindUniformGrid(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is UniformGrid itemGrid)
                    return itemGrid;

                UniformGrid? result = FindUniformGrid(child);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
