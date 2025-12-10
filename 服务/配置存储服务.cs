using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace AvaloniaApp八宝粥.服务;

/// <summary>
/// 负责把用户配置持久化到当前程序目录下的“配置文件/配置.json”。
/// </summary>
public static class 配置存储服务
{
    // 程序当前目录（可执行文件所在目录）
    private static readonly string 程序目录 =
        AppContext.BaseDirectory; // 等价于 exe 所在目录

    // 配置目录：{程序目录}\配置文件
    private static readonly string 配置目录 =
        Path.Combine(程序目录, "配置文件");

    // 配置文件路径：{程序目录}\配置文件\配置.json
    private static readonly string 配置文件路径 =
        Path.Combine(配置目录, "配置.json");

    /// <summary>
    /// 读取配置；如果文件不存在或解析失败，则返回默认配置。
    /// </summary>
    public static 用户配置模型 读取配置()
    {
        try
        {
            if (!File.Exists(配置文件路径)) return new 用户配置模型();

            var Json = File.ReadAllText(配置文件路径, Encoding.UTF8);
            var 读取结果 = JsonSerializer.Deserialize<用户配置模型>(Json, 获取序列化选项());

            return 读取结果 ?? new 用户配置模型();
        }
        catch
        {
            // 出异常时不要影响程序启动，直接给一份默认配置
            return new 用户配置模型();
        }
    }

    /// <summary>
    /// 保存配置到本地 JSON 文件。
    /// </summary>
    public static void 保存配置(用户配置模型 配置)
    {
        try
        {
            if (!Directory.Exists(配置目录)) Directory.CreateDirectory(配置目录);

            var Json = JsonSerializer.Serialize(配置, 获取序列化选项());

            File.WriteAllText(配置文件路径, Json, Encoding.UTF8);
        }
        catch
        {
            // 写配置失败就算了，不要让错误影响正常使用
        }
    }

    /// <summary>
    /// 统一 JSON 序列化配置：
    /// - 缩进格式化
    /// - 不转义中文（保持中文 key）
    /// </summary>
    private static JsonSerializerOptions 获取序列化选项()
    {
        return new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }
}