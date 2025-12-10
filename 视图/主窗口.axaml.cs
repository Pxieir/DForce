using Avalonia.Controls;
using AvaloniaApp八宝粥.视图模型;

namespace AvaloniaApp八宝粥.视图;

/// <summary>
/// 主窗口视图：仅负责初始化和绑定视图模型，界面布局在 XAML 中定义。
/// </summary>
public partial class 主窗口 : Window
{
    public 主窗口()
    {
        InitializeComponent();

        // 绑定主窗口视图模型，所有命令与数据绑定均从此对象暴露
        DataContext = new 主窗口视图模型();
    }
}