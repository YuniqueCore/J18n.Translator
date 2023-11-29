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


    [TestMethod]
    [DataRow(JsonData.test_Nested , 10)]
    public async Task DelayParseSelfRawTextTest(string json , int childrenCount)
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
        Assert.IsTrue(j18NJoint.Children.All(c => c.Children is null));
        var allExpanableNode = j18NJoint.Children.Where(c => c.Type != J18nJointType.String);
        foreach(var item in allExpanableNode)
        {
            item.ParseRawText(true);
        }
        //j18NJoint.Children.ElementAt(3).ParseRawText();
        Assert.IsTrue(j18NJoint.Children.ElementAt(3).Children.ElementAt(0).Children.Count == 6);
        Assert.IsTrue(j18NJoint.Children?.ElementAt(3).Children?.ElementAt(0).Children?.ElementAt(0).RawText == "1");
        Assert.IsTrue(allExpanableNode.All(c => c.Children is not null));
    }


    [TestMethod]
    [DataRow(JsonData.zh_CN , 8)]
    [DataRow(JsonData.en_US , 8)]
    [DataRow(JsonData.test_Nested , 10)]
    public async Task PathTest(string json , int childrenCount)
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
        Assert.IsTrue(j18NJoint.Children.All(c => c.Children is null));
        var allExpanableNode = j18NJoint.Children.Where(c => c.Type != J18nJointType.String);
        foreach(var item in allExpanableNode)
        {
            item.ParseRawText(true);
        }
        var arrayItem = j18NJoint.Children.Where(c => c.Type.Equals(J18nJointType.Array));
        var allRight = arrayItem.All(c =>
        {
            bool rightPath = true;
            for(int i = 0; i < c.Children.Count; i++)
            {
                rightPath = c.Children.ElementAt(i).Path == $"{c.Path}.[{i}]" && rightPath;
            }
            return rightPath;
        });
        Assert.IsTrue(allRight);
        //j18NJoint.Children.ElementAt(3).ParseRawText();
        Assert.IsTrue(allExpanableNode.All(c => c.Children is not null));
    }
}
