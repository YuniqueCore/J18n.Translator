using J18n.Test.TestData.Json;
using J18n.Translator;
using Newtonsoft.Json.Linq;

namespace J18n.Test.TestSuites;

[TestClass]
[TestCategory("JsonDiffer Test")]
public class JsonDifferTest
{
    [TestMethod]
    [DataRow(JsonData.zh_CN)]
    public void DeserializeByPathTest(string json)
    {
        IEnumerable<string> paths = new List<string>() { "description" , "nestedObject.name" };
        //J18nJoint j18NJoint = new J18nJoint()
        //{
        //    Index = 0 ,
        //    RawText = json ,
        //    Type = J18nJointType.Object ,
        //    Comment = "The root" ,
        //    Key = "root" ,
        //};
        var tokens = JsonDiffer.DeserializeByPath(json , paths);
        CollectionAssert.AllItemsAreNotNull(tokens);
        Assert.IsFalse(tokens.Any(kv => !kv.Key.EndsWith(((JProperty)kv.Value).Name)));
    }


    [TestMethod]
    [DataRow(JsonDiffData.initialJson , JsonDiffData.updatedJson)]
    public void ExtractInfoTest(string originalJson , string updatedJson)
    {
        var differences = Quibble.CSharp.JsonStrings.Diff(originalJson , updatedJson);
        JsonDiffer.ExtractInfo(differences , updatedJson);
        var aa = JsonDiffer.DiffHandlerManager.GetDiffHandlerChain(updatedJson);
    }
}
