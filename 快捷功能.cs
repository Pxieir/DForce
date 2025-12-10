using System.Diagnostics;
using AvaloniaApp八宝粥.视图模型;
using static AvaloniaApp八宝粥.服务.系统底层服务;

namespace AvaloniaApp八宝粥;

// 用于执行快捷功能指令的类
public static class 快捷功能
{
    public static async Task 执行启动程序指令()
    {
        var Obs进程名 = "obs64";
        var Obs程序路径 = @"D:\\Rice\\Downloads\\OBS\\obs-studio\\bin\\64bit\\obs64.exe";
        var Obs是否运行中 = false; // 记录本次是否启动了 OBS

        // 判断进程是否运行
        if (进程是否运行(Obs进程名))
        {
            主窗口视图模型.添加日志($"{Obs进程名} 已在运行，跳过分辨率更改");
        }
        else
        {
            // 如果 OBS 未运行，则执行完整的原始流程
            更改分辨率(1920, 1080);
            await Task.Delay(1000); // 等1秒
            启动指定程序(Obs程序路径);
            Obs是否运行中 = true;
        }

        // 如果是本次启动的 OBS，则等待“OBS Studio”弹窗出现并消失
        if (Obs是否运行中)
        {
            const string 弹窗标题 = "OBS Studio"; // 大小写敏感
            主窗口视图模型.添加日志($"等待弹窗 “{弹窗标题}” 出现");

            if (await 等待窗口出现(弹窗标题, TimeSpan.FromSeconds(60)))
            {
                主窗口视图模型.添加日志($"已检测到 “{弹窗标题}” ，继续等待其关闭");
                await 等待窗口消失(弹窗标题, TimeSpan.FromMinutes(2));
                主窗口视图模型.添加日志($"“{弹窗标题}” 已关闭");
            }
            else
            {
                主窗口视图模型.添加日志($"未在超时内检测到 “{弹窗标题}” ，继续后续流程");
            }

            // 更改分辨率(2560, 1600);
            更改分辨率(2560, 1600);
            await Task.Delay(2000);
        }

        // Steam
        检测并启动("steam", @"E:\\Program Files (x86)\\Steam\\steam.exe");
        // 微星小飞机
        检测并启动("MSIAfterburner", @"D:\\Program Files (x86)\\MSI Afterburner\\MSIAfterburner.exe");

        设置文件只读(@"C:\\ProgramData\\NVIDIA Corporation\\Drs\\nvdrsdb0.bin", true);
        设置文件只读(@"C:\\ProgramData\\NVIDIA Corporation\\Drs\\nvdrsdb1.bin", true);
        主窗口视图模型.添加日志("已设置 NVIDIA DRS 文件只读");

        主窗口视图模型.添加日志("--- 启动程序已完成 ---");
    }

    public static async Task 执行退出游戏指令()
    {
        结束指定进程("DeltaForceClient-Win64-Shipping.exe");
        结束指定进程("DeltaForceClient.exe");
        结束指定进程("df_launcher.exe");
        主窗口视图模型.添加日志("已结束八宝粥进程");
        await Task.Delay(3000); // 等待3秒

        检测并启动("HiBitUninstaller", @"D:\\Program Files (x86)\\HiBit Uninstaller\\HiBitUninstaller.exe");

        设置文件只读(@"C:\\ProgramData\\NVIDIA Corporation\\Drs\\nvdrsdb0.bin", false);
        设置文件只读(@"C:\\ProgramData\\NVIDIA Corporation\\Drs\\nvdrsdb1.bin", false);
        主窗口视图模型.添加日志("已取消 NVIDIA DRS 文件只读");


        主窗口视图模型.添加日志("--- 所有操作已执行完毕 ---");
    }


    // 检测指定名称的进程是否正在运行
    private static bool 进程是否运行(string 进程名称)
    {
        return Process.GetProcessesByName(进程名称).Length > 0;
    }

    // 根据提供的文件路径启动一个外部程序
    private static void 启动指定程序(string 程序文件路径)
    {
        if (!File.Exists(程序文件路径))
        {
            主窗口视图模型.添加日志($"错误：启动失败！找不到文件: {程序文件路径}");
            return;
        }

        try
        {
            ProcessStartInfo 启动信息 = new()
            {
                FileName = 程序文件路径,
                // 设置工作目录为程序文件所在的目录，确保程能找到其相对路径下的资源文件
                WorkingDirectory = Path.GetDirectoryName(程序文件路径),
                UseShellExecute = true
            };

            Process.Start(启动信息);
        }
        catch (Exception Ex)
        {
            主窗口视图模型.添加日志($"错误：启动 {Path.GetFileName(程序文件路径)} 失败: {Ex.Message}");
        }
    }

    // 检测一个程序是否在运行，如果不在，则启动它
    private static void 检测并启动(string 程序名称, string 程序路径)
    {
        if (!进程是否运行(程序名称))
        {
            主窗口视图模型.添加日志($"{程序名称} 正在启动");
            启动指定程序(程序路径);
        }
        else
        {
            主窗口视图模型.添加日志($"{程序名称} 已在运行");
        }
    }

    // 结束指定名称的所有进程
    private static void 结束指定进程(string 进程名称)
    {
        var 纯进程名 = Path.GetFileNameWithoutExtension(进程名称);
        try
        {
            var 进程列表 = Process.GetProcessesByName(纯进程名);
            if (进程列表.Length <= 0) return;
            foreach (var 进程 in 进程列表) 进程.Kill();

            主窗口视图模型.添加日志($"已结束所有 {进程名称} 进程");
        }
        catch (Exception)
        {
            /* 忽略错误 */
        }
    }

    // 设置或取消文件的只读属性
    private static void 设置文件只读(string 文件路径, bool 设为只读)
    {
        try
        {
            if (!File.Exists(文件路径))
            {
                主窗口视图模型.添加日志($"警告：找不到文件，无法设置属性: {文件路径}");
                return;
            }

            var 属性 = File.GetAttributes(文件路径);
            if (设为只读)
                File.SetAttributes(文件路径, 属性 | FileAttributes.ReadOnly);
            else
                File.SetAttributes(文件路径, 属性 & ~FileAttributes.ReadOnly);
        }
        catch (Exception Ex)
        {
            主窗口视图模型.添加日志($"错误：修改文件属性失败: {Ex.Message}");
        }
    }
}