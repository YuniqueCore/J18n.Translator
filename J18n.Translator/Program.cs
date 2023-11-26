using Newtonsoft.Json.Linq;
using Serilog;

namespace J18n.Translator;

public class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        Log.Logger = new LoggerConfiguration()
            .CreateLogger();

        // 示例日志记录
        Log.Information("Hello, Serilog!");

        Log.CloseAndFlush();  // 关闭并刷新日志记录器
    }
}


public class J18nParser
{
    public static async Task<JToken?> ParseJsonToJ18nRootAsync(string json , CancellationToken cToken)
    {
        var root = (await TaskManager.RunTask<JObject?>(( ) => JObject.Parse(json) , cToken))?.Root;
        return root;
    }

}


