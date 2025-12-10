namespace AvaloniaApp八宝粥.服务;

public class 压枪核心服务
{
    private readonly 运行缓存 _缓存 = new();
    private readonly Random _随机数生成器 = new();

    // 事件定义：用于通知 UI 更新
    public event Action<string>? 日志更新事件;

    /// <summary>
    ///     启动后台压枪任务
    /// </summary>
    public async Task 运行压枪循环(用户配置模型 配置, CancellationToken 取消令牌)
    {
        日志更新事件?.Invoke("后台服务：压枪循环已启动，等待触发条件...");

        try
        {
            while (!取消令牌.IsCancellationRequested)
            {
                // 1. 获取异步按键状态动态获取当前配置的热键码
                var 热键码 = 获取热键码(配置.启动控制键);

                // 2. 获取异步按键状态使用动态获取的 热键码
                var 启动键状态 = 系统底层服务.检测按键开关状态(热键码);
                var 左键状态 = 系统底层服务.检测按键按下(系统底层服务.虚拟键左键);
                var 右键状态 = 系统底层服务.检测按键按下(系统底层服务.虚拟键右键);

                if (启动键状态 && 左键状态 && 右键状态) await 执行开火逻辑(配置, 取消令牌, 热键码); // 传入热键码

                // 降低空闲 CPU 占用
                await Task.Delay(10, 取消令牌);
            }
        }
        catch (TaskCanceledException)
        {
            日志更新事件?.Invoke("后台服务：任务已取消");
        }
        catch (Exception Ex)
        {
            日志更新事件?.Invoke($"后台服务异常：{Ex.Message}");
        }
    }

    // 获取异步按键状态增加热键码参数
    private async Task 执行开火逻辑(用户配置模型 配置, CancellationToken 取消令牌, int 热键码)
    {
        // 初始化
        var 起始坐标 = 系统底层服务.获取鼠标坐标();
        _缓存.上一次y轴位置 = 起始坐标.Y;
        _缓存.当前偏差像素 = 0;
        _缓存.累计偏差像素 = 0;

        // 获取异步按键状态循环判断条件中使用传入的 热键码
        while (!取消令牌.IsCancellationRequested &&
               系统底层服务.检测按键开关状态(热键码) &&
               系统底层服务.检测按键按下(系统底层服务.虚拟键左键) &&
               系统底层服务.检测按键按下(系统底层服务.虚拟键右键))
        {
            // 1. 随机偏差计算
            var 随机步长 = _随机数生成器.Next(-1, 2);
            _缓存.当前偏差像素 = Math.Clamp(
                _缓存.当前偏差像素 + 随机步长,
                -配置.随机偏差范围,
                配置.随机偏差范围
            );

            // 2. 计算理论力度
            var 理论力度像素 = 计算理论力度(配置);

            // 3. 执行移动
            系统底层服务.移动鼠标相对位置(0, 理论力度像素);

            // 4. 随机延迟
            await Task.Delay(_随机数生成器.Next(15, 18), 取消令牌);

            // 5. 误差修正与数据反馈
            更新位置跟踪与误差(理论力度像素);
        }
    }

    private int 计算理论力度(用户配置模型 配置)
    {
        var 修正值 = _缓存.累计偏差像素 / (配置.脱敏度 == 0 ? 1 : 配置.脱敏度); // 防止除零
        return (int)Math.Round(配置.基础力度 + _缓存.当前偏差像素 + 修正值);
    }

    private void 更新位置跟踪与误差(int 理论移动像素)
    {
        var 当前坐标 = 系统底层服务.获取鼠标坐标();
        var 实际移动像素 = 当前坐标.Y - _缓存.上一次y轴位置;

        // 计算本次误差
        var 本次误差 = 实际移动像素 - 理论移动像素;
        _缓存.累计偏差像素 += 本次误差;
        _缓存.上一次y轴位置 = 当前坐标.Y;
    }

    // 根据字符串返回对应的虚拟键码
    private static int 获取热键码(string 键名)
    {
        return 键名 switch
        {
            "CapsLock" => 系统底层服务.虚拟键Capslock,
            "NumLock" => 系统底层服务.虚拟键NumLock,
            _ => 系统底层服务.虚拟键Capslock // 默认值
        };
    }
}