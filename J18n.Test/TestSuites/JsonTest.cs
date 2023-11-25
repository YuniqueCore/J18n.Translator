using J18n.Test.TestData.Json;
using J18n.Translator;

namespace J18n.Test.TestSuites;

[TestClass]
[TestCategory("Json Test")]
public class JsonTest
{
    private static CancellationTokenSource? _cts;
    private static volatile object _lock = new object();

    public static CancellationTokenSource CTS
    {
        get
        {
            if(_cts is null)
            {
                lock(_lock)
                {
                    return _cts ?? new CancellationTokenSource();
                }
            }

            return _cts;
        }
    }

    [TestInitialize]
    public void InitTest( )
    {
        var init_cts = CTS;
    }


    [TestMethod]
    [DataRow(JsonData.test_Nested)]
    public async Task DeserializeJsonTest(string json)
    {
        var J18nObject = await J18nParser.ParseJsonToJ18nRootAsync(json , CTS.Token);
        Assert.IsTrue(J18nObject?.Children().Count() == 10);
    }
}
