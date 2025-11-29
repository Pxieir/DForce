using System.Runtime.InteropServices;
using AvaloniaApp八宝粥.视图模型;

namespace AvaloniaApp八宝粥;

/// <summary>
///     封装 Windows API，对外提供中文方法
/// </summary>
public static class 系统底层服务
{
    #region 鼠标控制

    private const uint InputMouse = 0;
    private const uint MouseeventfMove = 0x0001;

    // --- 常量定义 ---
    public const int 虚拟键左键 = 0x01;
    public const int 虚拟键右键 = 0x02;

    public const int 虚拟键Capslock = 0x14;
    public const int 虚拟键NumLock = 0x90;

    // --- P/Invoke 定义 (保留英文 EntryPoint 以匹配系统导出) ---
    [DllImport("user32.dll", EntryPoint = "GetAsyncKeyState")]
    private static extern short 获取异步按键状态(int vKey);

    [DllImport("user32.dll", EntryPoint = "GetKeyState")]
    private static extern short 获取按键状态(int nVirtKey);

    [DllImport("user32.dll", EntryPoint = "GetCursorPos")]
    private static extern bool 获取异步按键状态(out Point lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

    // --- 中文包装方法 ---
    public static bool 检测按键按下(int 虚拟键码)
    {
        return (获取异步按键状态(虚拟键码) & 0x8000) != 0;
    }

    public static bool 检测按键开关状态(int 虚拟键码)
    {
        return (获取按键状态(虚拟键码) & 0x0001) != 0;
    }

    /// <summary>
    ///     使用 SendInput 相对移动鼠标
    /// </summary>
    public static void 移动鼠标相对位置(int x, int y)
    {
        var Inputs = new Input[1];
        Inputs[0].type = InputMouse;
        Inputs[0].mi = new Mouseinput
        {
            dx = x,
            dy = y,
            mouseData = 0,
            dwFlags = MouseeventfMove,
            time = 0,
            dwExtraInfo = IntPtr.Zero
        };

        SendInput((uint)Inputs.Length, Inputs, Marshal.SizeOf(typeof(Input)));
    }

    public static (int X, int Y) 获取鼠标坐标()
    {
        return 获取异步按键状态(out var 点) ? (点.X, 点.Y) : (0, 0);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    // --- SendInput 相关结构 ---
    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint type;
        public Mouseinput mi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Mouseinput
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    #endregion


    #region 分辨率更改

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