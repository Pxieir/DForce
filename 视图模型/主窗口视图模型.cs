using System.Collections.ObjectModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaApp八宝粥.视图模型;

public partial class 主窗口视图模型 : ObservableObject
{
    private readonly 压枪核心服务 _压枪服务;

    // 属性绑定
    [ObservableProperty] private 用户配置模型 _当前配置;
    private CancellationTokenSource? _取消令牌源;
    [ObservableProperty] private string _运行状态文本 = "已停止";
    [ObservableProperty] private bool _正在运行;
    [ObservableProperty] private bool _正在等待恢复;
    [ObservableProperty] private string _快捷按钮文本 = "启动程序";

    // 构造函数
    public 主窗口视图模型()
    {
        _当前配置 = new 用户配置模型();
        _压枪服务 = new 压枪核心服务();
        _压枪服务.日志更新事件 += 添加日志;
        添加日志("程序已初始化");
    }

    private static ObservableCollection<string> 日志列表 { get; } = [];

    // 命令
    [RelayCommand]
    private async Task 切换运行状态()
    {
        if (正在运行)
            await 停止服务();
        else
            await 启动服务();
    }

    [RelayCommand]
    private async Task 保存配置()
    {
        try
        {
            // 确保目录存在
            var 文件夹 = Path.Combine(Environment.CurrentDirectory, "配置文件");
            if (!Directory.Exists(文件夹)) Directory.CreateDirectory(文件夹);

            var 文件路径 = Path.Combine(文件夹, "配置.json");

            // 设置序列化选项，保证中文不转义
            var 选项 = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };

            var Json = JsonSerializer.Serialize(当前配置, 选项);
            await File.WriteAllTextAsync(文件路径, Json);

            添加日志($"配置已保存到 {文件路径}");
        }
        catch (Exception Ex)
        {
            添加日志($"保存失败: {Ex.Message}");
        }
    }

    [RelayCommand]
    private async Task 加载配置()
    {
        try
        {
            var 文件夹 = Path.Combine(Environment.CurrentDirectory, "配置文件");
            var 文件路径 = Path.Combine(文件夹, "配置.json");

            if (File.Exists(文件路径))
            {
                var Json = await File.ReadAllTextAsync(文件路径);
                var 加载的配置 = JsonSerializer.Deserialize<用户配置模型>(Json);
                if (加载的配置 != null)
                {
                    当前配置 = 加载的配置;
                    添加日志("配置已加载");
                }
            }
            else
            {
                添加日志("未找到配置文件");
            }
        }
        catch (Exception Ex)
        {
            添加日志($"加载失败: {Ex.Message}");
        }
    }

    [RelayCommand]
    private static async Task 导出日志()
    {
        try
        {
            // 确保目录存在
            var 文件夹 = Path.Combine(Environment.CurrentDirectory, "日志");
            if (!Directory.Exists(文件夹)) Directory.CreateDirectory(文件夹);

            // 日志文件完整路径：日志文件夹/logs_时间戳.txt
            var 文件路径 = Path.Combine(文件夹, $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            await File.WriteAllLinesAsync(文件路径, 日志列表);
            添加日志($"日志已导出到 {文件路径}");
        }
        catch (Exception Ex)
        {
            添加日志($"导出日志失败: {Ex.Message}");
        }
    }

    [RelayCommand]
    private void 加载示例配置(string 类型)
    {
        switch (类型)
        {
            case "轻":
                当前配置.基础力度 = 3;
                当前配置.脱敏度 = 60;
                当前配置.随机偏差范围 = 1;
                break;
            case "中":
                当前配置.基础力度 = 6;
                当前配置.脱敏度 = 45;
                当前配置.随机偏差范围 = 2;
                break;
            case "重":
                当前配置.基础力度 = 10;
                当前配置.脱敏度 = 30;
                当前配置.随机偏差范围 = 3;
                break;
        }

        添加日志($"已加载示例配置：{类型}");
    }


    [RelayCommand]
    private void 切换快捷功能按钮()
    {
        if (!正在等待恢复)
        {
            快捷功能.执行启动程序指令();
            快捷按钮文本 = "恢复分辨率";
            正在等待恢复 = true;
            添加日志("启动程序1执行完毕，等待恢复分辨率");
        }
        else
        {
            快捷功能.执行恢复分辨率指令();
            快捷按钮文本 = "启动程序";
            正在等待恢复 = false;
            添加日志("启动程序2执行完毕，已恢复分辨率");
        }
    }

    // 内部逻辑
    private Task 启动服务()
    {
        if (正在运行) return Task.CompletedTask;

        _取消令牌源 = new CancellationTokenSource();
        正在运行 = true;
        运行状态文本 = "运行中";
        添加日志("=== 服务启动 ===");

        // 在后台线程运行
        _ = Task.Run((Func<Task?>)(() => _压枪服务.运行压枪循环(当前配置, _取消令牌源.Token)));
        return Task.CompletedTask;
    }

    private async Task 停止服务()
    {
        if (!正在运行 || _取消令牌源 == null) return;

        await _取消令牌源.CancelAsync();
        正在运行 = false;
        运行状态文本 = "已停止";
        添加日志("=== 服务停止 ===");
    }

    public static void 添加日志(string 内容)
    {
        // 确保在 UI 线程更新
        Dispatcher.UIThread.Post(() =>
        {
            var 时间戳 = DateTime.Now.ToString("HH:mm:ss");
            日志列表.Insert(0, $"[{时间戳}] {内容}");
            // 限制日志数量
            if (日志列表.Count > 100) 日志列表.RemoveAt(日志列表.Count - 1);
        });
    }
}