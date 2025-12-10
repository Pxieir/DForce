using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaApp八宝粥;

/// <summary>
///     用户配置数据模型，支持属性变更通知
/// </summary>
public partial class 用户配置模型 : ObservableObject
{
    [ObservableProperty] private int _基础力度 = 5;

    [ObservableProperty] private int _脱敏度 = 50;

    [ObservableProperty] private int _随机偏差范围 = 1;

    [ObservableProperty] private int _屏幕高度 = 1600;

    [ObservableProperty] private string _启动控制键 = "CapsLock";
}

/// <summary>
///     运行时缓存数据
/// </summary>
public class 运行缓存
{
    public int 当前偏差像素 { get; set; }
    public double 累计偏差像素 { get; set; }
    public int 上一次y轴位置 { get; set; }
}