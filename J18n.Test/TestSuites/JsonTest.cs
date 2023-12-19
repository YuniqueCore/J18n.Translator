using J18n.Test.TestData.Json;
using J18n.Translator;
using Newtonsoft.Json.Linq;

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
        var J18nObject = JObject.Parse(json);
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
        j18NJoint.UpdateRawTextAndChildren(j18NJoint.RawText , true);
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
        j18NJoint.UpdateRawTextAndChildren(j18NJoint.RawText , false);
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
        Assert.IsTrue(j18NJoint.Children?.Count == childrenCount , $"The Children count {j18NJoint.Children?.Count} should equals {childrenCount}");
        Assert.IsTrue(j18NJoint.Children?.All(c => c.Children is null) , $"Children should be null when recursive parse is false.");
        var allExpanableNode = j18NJoint.Children?.Where(c => c.Type != J18nJointType.String);
        foreach(var item in allExpanableNode!)
        {
            item.ParseRawText(true);
        }
        var arrayItem = j18NJoint.Children?.Where(c => c.Type.Equals(J18nJointType.Array));
        var allRight = arrayItem?.All(c =>
        {
            bool rightPath = true;
            for(int i = 0; i < c.Children?.Count; i++)
            {
                // The [index] array items path was handled in the nested ParseArray method of J18nJoint.ParseRawText(bool recursive) method.
                rightPath = c.Children.ElementAt(i).Path == $"{c.Path}[{i}]" && rightPath;
            }
            return rightPath;
        });
        Assert.IsTrue(allRight.HasValue && allRight.Value , $"All Array children Items path should end with the formatted string: ...[index], are all match rule? {allRight}");
        //j18NJoint.Children.ElementAt(3).ParseRawText();
        Assert.IsTrue(allExpanableNode.All(c => c.Children is not null) , "All expandable node should have children.");
    }





    [TestMethod]
    [DataRow(nameof(CommonTestData.parsePathToKeysDatas))]
    public void ParsePathFromStringTest(string dataSource)
    {
        foreach(var item in CommonTestData.parsePathToKeysDatas)
        {
            var path = item.JointPath.TrimStart('.');
            var keys = item.Keys;

            var result = J18nJointExtension.ParsePath(path);
            for(int i = 0; i < keys.Length; i++)
            {
                Assert.AreEqual(keys[i] , result.ElementAt(i) ,
                    $"expected key: {keys[i]} should equal actual key {result.ElementAt(i)}");
            }
        }
    }








}
