using System.Diagnostics;
using System.Runtime.InteropServices;
using AvaloniaApp八宝粥.视图模型;

namespace AvaloniaApp八宝粥;

// 用于执行快捷功能指令的类
public static class 快捷功能
{
    public static void 执行启动程序指令()
    {
        var Obs进程名 = "obs64";
        var Obs程序路径 = @"D:\\Rice\\Downloads\\OBS\\obs-studio\\bin\\64bit\\obs64.exe";

        // 判断进程是否运行
        if (进程是否运行(Obs进程名))
        {
            主窗口视图模型.添加日志($"检测到 {Obs进程名} 已在运行，跳过分辨率更改");
            主窗口视图模型.添加日志("\n第一阶段已完成，请点击\"恢复分辨率\"继续执行后续步骤");
        }
        else
        {
            // 如果 OBS 未运行，则执行完整的原始流程
            主窗口视图模型.添加日志("--- 设置 1K 分辨率并启动 OBS ---");
            更改分辨率(1920, 1080);
            Thread.Sleep(1000); // 等1秒
            启动指定程序(Obs程序路径);

            主窗口视图模型.添加日志("\n第一阶段已完成，请点击\"恢复分辨率\"继续执行后续步骤");
        }
    }

    public static void 执行恢复分辨率指令()
    {
        主窗口视图模型.添加日志("\n--- 恢复 2.5K 分辨率 ---");
        更改分辨率(2560, 1600);
        Thread.Sleep(2000); // 等2秒

        主窗口视图模型.添加日志("\n正在启动程序");

        // Steam
        检测并启动("steam", @"E:\\Program Files (x86)\\Steam\\steam.exe");
        // 微星小飞机
        检测并启动("MSIAfterburner", @"D:\\Program Files (x86)\\MSI Afterburner\\MSIAfterburner.exe");

        主窗口视图模型.添加日志("\n初始软件就绪，脚本已暂停");
        // Console.ReadLine();

        主窗口视图模型.添加日志("\n正在设置 NVIDIA DRS 文件为只读");
        设置文件只读(@"C:\\ProgramData\\NVIDIA Corporation\\Drs\\nvdrsdb0.bin", true);
        设置文件只读(@"C:\\ProgramData\\NVIDIA Corporation\\Drs\\nvdrsdb1.bin", true);

        主窗口视图模型.添加日志("\n初始软件就绪，脚本已暂停");
        主窗口视图模型.添加日志("按任意键继续执行后续的清理和卸载步骤");
        // Console.ReadLine();

        主窗口视图模型.添加日志("\n正在结束指定的软件进程");
        结束指定进程("DeltaForceClient-Win64-Shipping.exe");
        结束指定进程("DeltaForceClient.exe");
        结束指定进程("df_launcher.exe");

        主窗口视图模型.添加日志("\n进程清理完毕，等待3秒");
        Thread.Sleep(3000); // 等待3秒

        主窗口视图模型.添加日志("\n正在启动卸载程序");
        检测并启动("HiBitUninstaller", @"D:\\Program Files (x86)\\HiBit Uninstaller\\HiBitUninstaller.exe");

        主窗口视图模型.添加日志("\n正在取消 NVIDIA DRS 文件的只读状态");
        设置文件只读(@"C:\\ProgramData\\NVIDIA Corporation\\Drs\\nvdrsdb0.bin", false);
        设置文件只读(@"C:\\ProgramData\\NVIDIA Corporation\\Drs\\nvdrsdb1.bin", false);

        主窗口视图模型.添加日志("\n--- 所有操作已执行完毕 ---");
    }

    #region 核心功能方法

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
            主窗口视图模型.添加日志($"已发送启动指令: {Path.GetFileName(程序文件路径)}");
        }
        catch (Exception Ex)
        {
            主窗口视图模型.添加日志($"错误：启动 {Path.GetFileName(程序文件路径)} 失败: {Ex.Message}");
        }
    }


    // 检测一个程序是否在运行，如果不在，则启动它
    private static void 检测并启动(string 程序名称, string 程序路径)
    {
        主窗口视图模型.添加日志($"正在检测 {程序名称}");
        if (!进程是否运行(程序名称))
        {
            主窗口视图模型.添加日志($"{程序名称} 未运行，正在启动");
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
            {
                File.SetAttributes(文件路径, 属性 | FileAttributes.ReadOnly);
                主窗口视图模型.添加日志($"已将 {Path.GetFileName(文件路径)} 设置为只读");
            }
            else
            {
                File.SetAttributes(文件路径, 属性 & ~FileAttributes.ReadOnly);
                主窗口视图模型.添加日志($"已取消 {Path.GetFileName(文件路径)} 的只读状态");
            }
        }
        catch (Exception Ex)
        {
            主窗口视图模型.添加日志($"错误：修改文件属性失败: {Ex.Message}");
        }
    }

    #endregion

    #region 分辨率更改相关代码

    [DllImport("user32.dll", EntryPoint = "ChangeDisplaySettings")]
    private static extern int 更改显示设置(ref 设备模式 devMode, int flags);

    private const int 更新注册表 = 0x01;
    private const int 更改成功 = 0;
    private const int 需要重启 = 1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct 设备模式
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;

        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;

        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    public const int 像素宽度 = 0x00080000;
    public const int 像素高度 = 0x00100000;

    public static void 更改分辨率(int 宽度, int 高度)
    {
        var 设备模式实例 = new 设备模式
        {
            dmSize = (short)Marshal.SizeOf(typeof(设备模式)),
            dmPelsWidth = 宽度,
            dmPelsHeight = 高度,
            dmFields = 像素宽度 | 像素高度
        };
        var 函数执行结果 = 更改显示设置(ref 设备模式实例, 更新注册表);
        switch (函数执行结果)
        {
            case 更改成功:
                主窗口视图模型.添加日志($"分辨率已成功更改为 {宽度}x{高度}");
                break;
            case 需要重启:
                主窗口视图模型.添加日志("提示：需要重启才能应用新的分辨率设置");
                break;
            default:
                主窗口视图模型.添加日志($"错误：更改分辨率 {宽度}x{高度} 失败");
                break;
        }
    }

    #endregion
}
