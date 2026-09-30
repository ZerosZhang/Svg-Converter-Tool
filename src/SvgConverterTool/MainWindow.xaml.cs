using SvgConverterTool.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SvgConverterTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // 版本号取自程序集，避免与 csproj 中的 Version 不一致
            TextVersion.Text = $"v{typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "0.0.0"}";
            // 默认选中第一个导航按钮
            SetActiveNavButton(NavSvgConvert);
        }

        /// <summary>
        /// 导航按钮点击事件
        /// </summary>
        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            SetActiveNavButton(btn);

            string tag = btn.Tag?.ToString() ?? string.Empty;
            ContentArea.Content = tag switch
            {
                "IcoConvert" => new IcoConvertView(),
                _ => new SvgConvertView()
            };
        }

        /// <summary>
        /// 设置当前激活的导航按钮样式
        /// </summary>
        private void SetActiveNavButton(Button activeButton)
        {
            // 重置所有导航按钮样式
            ResetNavButtonStyle(NavSvgConvert);
            ResetNavButtonStyle(NavIcoConvert);

            // 设置激活状态
            activeButton.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)); // #3B82F6
            activeButton.Foreground = Brushes.White;
        }

        /// <summary>
        /// 重置导航按钮为默认样式
        /// </summary>
        private static void ResetNavButtonStyle(Button btn)
        {
            btn.Background = Brushes.Transparent;
            btn.Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 204)); // #CCCCCC
        }
    }
}
