using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace J18n.Translator;

public class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}

public class J18nNode : JObject
{

}


public class J18nParser
{
    public static async Task<object?> ParseJsonToJ18n(string json , CancellationToken cToken)
    {
        return await Task.Run(( ) =>
         {
             var j18nRoot = JsonConvert.DeserializeObject(json);
             return j18nRoot;
         } , cToken);
    }

}
