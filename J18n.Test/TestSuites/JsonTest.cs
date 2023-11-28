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
    public async Task DeserializeJsonByNewtonsoftTest(string json)
    {
        var J18nObject = await J18nParser.ParseJsonToJ18nRootAsync(json , CTS.Token);
        Assert.IsTrue(J18nObject?.Children().Count() == 10);
    }

    [TestMethod]
    [DataRow(JsonData.zh_CN , 8)]
    [DataRow(JsonData.en_US , 8)]
    [DataRow(JsonData.test_Nested , 10)]
    public async Task DeserializeJsonByJ18nJointTest(string json , int childrenCount)
    {
        J18nJoint j18NJoint = new J18nJoint()
        {
            Index = 0 ,
            RawText = json ,
            Type = J18nJointType.Object ,
            Comment = "The root" ,
            Key = "root" ,
        };
        j18NJoint.ParseRawTextToChildren(j18NJoint.RawText , true);
        Assert.IsTrue(j18NJoint.Children.Count() == childrenCount);
        Assert.AreEqual(j18NJoint.Children.Any(c => !c.Parent.Equals(j18NJoint)) , false);
    }

    [TestMethod]
    [DataRow(JsonData.zh_CN , 8)]
    [DataRow(JsonData.en_US , 8)]
    [DataRow(JsonData.test_Nested , 10)]
    public async Task DeserializeJsonByJ18nJointWithoutRecursiveTest(string json , int childrenCount)
    {
        J18nJoint j18NJoint = new J18nJoint()
        {
            Index = 0 ,
            RawText = json ,
            Type = J18nJointType.Object ,
            Comment = "The root" ,
            Key = "root" ,
        };
        j18NJoint.ParseRawTextToChildren(j18NJoint.RawText , false);
        Assert.IsTrue(j18NJoint.Children.Count() == childrenCount);
        Assert.AreEqual(j18NJoint.Children.Any(c => !c.Parent.Equals(j18NJoint)) , false);
    }

    [TestMethod]
    [DataRow(JsonData.zh_CN , 8)]
    [DataRow(JsonData.en_US , 8)]
    [DataRow(JsonData.test_Nested , 10)]
    public async Task ParseSelfRawTextTest(string json , int childrenCount)
    {
        J18nJoint j18NJoint = new J18nJoint()
        {
            Index = 0 ,
            RawText = json ,
            Type = J18nJointType.Object ,
            Comment = "The root" ,
            Key = "root" ,
        };
        j18NJoint.ParseRawText(false);
        Assert.IsTrue(j18NJoint.Children.Count() == childrenCount);
        Assert.AreEqual(j18NJoint.Children.Any(c => !c.Parent.Equals(j18NJoint)) , false);

        j18NJoint.ParseRawText(true);
        Assert.IsTrue(j18NJoint.Children.Count() == childrenCount);
        Assert.AreEqual(j18NJoint.Children.Any(c => !c.Parent.Equals(j18NJoint)) , false);
    }

    [TestMethod]
    [DataRow(JsonData.zh_CN , 8)]
    [DataRow(JsonData.en_US , 8)]
    [DataRow(JsonData.test_Nested , 10)]
    public async Task ParseSelfRawTextWithoutRawTextTest(string json , int childrenCount)
    {
        J18nJoint j18NJoint = new J18nJoint()
        {
            Index = 0 ,
            //RawText = json ,
            Type = J18nJointType.Object ,
            Comment = "The root" ,
            Key = "root" ,
        };
        j18NJoint.ParseRawText(false);
        Assert.IsTrue(j18NJoint.Children is null);
    }
}
