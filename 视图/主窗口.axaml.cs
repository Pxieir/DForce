using Avalonia.Controls;
using AvaloniaApp八宝粥.视图模型;

namespace AvaloniaApp八宝粥.视图;

public partial class 主窗口 : Window
{
    public 主窗口()
    {
        InitializeComponent();
        DataContext = new 主窗口视图模型();
    }
}