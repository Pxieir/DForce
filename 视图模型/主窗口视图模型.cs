using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AvaloniaApp八宝粥.服务;


namespace AvaloniaApp八宝粥.视图模型;

/// <summary>
/// 主窗口视图模型：
/// - 负责驱动压枪核心服务
/// - 负责调度一键启动 / 一键退出快捷功能
/// - 负责承接底层服务与静态调用的日志输出
/// </summary>
public partial class 主窗口视图模型 : ObservableObject
{
    /// <summary>
    /// 当前正在使用的视图模型实例，
    /// 供 快捷功能 / 其他静态调用通过 <see cref="添加日志"/> 输出日志。
    /// </summary>
    private static 主窗口视图模型? _当前实例;

    /// <summary>
    /// 压枪逻辑服务（后台循环逻辑）
    /// </summary>
    private readonly 压枪核心服务 _压枪服务 = new();

    /// <summary>
    /// 压枪循环的取消令牌
    /// </summary>
    private CancellationTokenSource? _压枪取消源;

    /// <summary>
    /// 用户可调参数（基础力度 / 屏幕高度 / 随机偏差 / 脱敏度 / 启动键）
    /// </summary>
    public 用户配置模型 配置 { get; } = 配置存储服务.读取配置();

    /// <summary>
    /// 内部日志集合（可写）
    /// </summary>
    private readonly ObservableCollection<string> _日志集合 = [];

    /// <summary>
    /// 供界面绑定的只读日志集合
    /// </summary>
    public ReadOnlyObservableCollection<string> 日志条目 { get; }

    /// <summary>
    /// 是否处于压枪运行状态，用于控制按钮文字与状态显示。
    /// </summary>
    [ObservableProperty] private bool _是否压枪运行中;

    /// <summary>
    /// 当前状态提示文本（显示在核心操作卡片底部）
    /// </summary>
    [ObservableProperty] private string _状态文本 = "已就绪";

    /// <summary>
    /// 主操作按钮文字：根据压枪状态自动切换“开始压枪”/“停止压枪”
    /// </summary>
    public string 压枪按钮文本 => 是否压枪运行中 ? "停止压枪" : "开始压枪";

    public 主窗口视图模型()
    {
        _当前实例 = this;

        日志条目 = new ReadOnlyObservableCollection<string>(_日志集合);

        _压枪服务.日志更新事件 += 添加日志;

        // ★ 当配置任意属性发生变化时，自动保存到本地
        配置.PropertyChanged += (_, _) => { 配置存储服务.保存配置(配置); };

        添加日志("应用已启动，等待操作...");
    }


    /// <summary>
    /// 当压枪状态改变时，通知按钮文字也更新。
    /// </summary>
    partial void On是否压枪运行中Changed(bool value)
    {
        OnPropertyChanged(nameof(压枪按钮文本));
    }

    /// <summary>
    /// 供底层静态调用（如 快捷功能 / 其他服务）输出日志的入口。
    /// 调用示例：主窗口视图模型.添加日志("xxx");
    /// </summary>
    public static void 添加日志(string 消息)
    {
        _当前实例?.在ui线程追加日志(消息);
    }

    /// <summary>
    /// 在 UI 线程安全地向日志集合追加一条记录，并更新状态文本。
    /// </summary>
    private void 在ui线程追加日志(string 消息)
    {
        if (string.IsNullOrWhiteSpace(消息))
            return;

        if (Dispatcher.UIThread.CheckAccess())
            执行();
        else
            Dispatcher.UIThread.Post(执行);

        return;

        void 执行()
        {
            // 控制日志长度，避免内存无限增长
            if (_日志集合.Count > 500) _日志集合.RemoveAt(0);

            _日志集合.Add($"{DateTime.Now:HH:mm:ss}  {消息}");
            状态文本 = 消息;
        }
    }

    /// <summary>
    /// 切换压枪状态：
    /// - 若当前未运行：启动后台压枪循环
    /// - 若当前已运行：取消后台任务
    /// </summary>
    [RelayCommand]
    private async Task 切换压枪()
    {
        if (是否压枪运行中)
        {
            // 已在运行 -> 停止
            _压枪取消源?.Cancel();
            添加日志("已请求停止压枪循环");
            是否压枪运行中 = false;
            await Task.CompletedTask;
            return;
        }

        // 未运行 -> 启动
        _压枪取消源 = new CancellationTokenSource();
        是否压枪运行中 = true;
        添加日志("压枪循环启动中...");

        var 令牌 = _压枪取消源.Token;

        // 后台运行压枪循环，避免阻塞 UI 线程
        _ = Task.Run(async () =>
        {
            try
            {
                await _压枪服务.运行压枪循环(配置, 令牌);
            }
            catch (Exception Ex)
            {
                添加日志($"压枪循环发生异常：{Ex.Message}");
            }
            finally
            {
                // 循环结束时，无论正常退出还是异常，都重置状态
                是否压枪运行中 = false;
                添加日志("压枪循环已结束");
            }
        }, 令牌);
    }

    /// <summary>
    /// 一键启动指令：
    /// 调用 快捷功能.执行启动程序指令()
    /// </summary>
    [RelayCommand]
    private async Task 执行一键启动()
    {
        添加日志("正在执行一键启动流程...");
        try
        {
            await 快捷功能.执行启动程序指令();
            添加日志("一键启动流程已完成");
        }
        catch (Exception Ex)
        {
            添加日志($"一键启动失败：{Ex.Message}");
        }
    }

    /// <summary>
    /// 一键退出指令：
    /// 调用 快捷功能.执行退出游戏指令()
    /// </summary>
    [RelayCommand]
    private async Task 执行一键退出()
    {
        添加日志("正在执行一键退出流程...");
        try
        {
            await 快捷功能.执行退出游戏指令();
            添加日志("一键退出流程已完成");
        }
        catch (Exception Ex)
        {
            添加日志($"一键退出失败：{Ex.Message}");
        }
    }
}