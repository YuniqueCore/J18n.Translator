using Newtonsoft.Json.Linq;

namespace J18n.Translator;

public class Program
{
    private static void Main(string[] args)
    {

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


