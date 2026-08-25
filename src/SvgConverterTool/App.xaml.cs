using System.IO;
using System.Windows;
using SvgConverterTool.Views;

namespace SvgConverterTool;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string icoPath = Path.Combine(baseDir, "app.ico");
        string svgPath = Path.Combine(baseDir, "External", "file-svg.svg");

        // 如果 app.ico 不存在但 file-svg.svg 存在，则自动生成图标
        if (!File.Exists(icoPath) && File.Exists(svgPath))
        {
            try
            {
                SvgConvertView.ConvertSvgToIco(svgPath, icoPath);
            }
            catch
            {
                // 忽略图标生成错误，不影响主程序运行
            }
        }
    }
}
