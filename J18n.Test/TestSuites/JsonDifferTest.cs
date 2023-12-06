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

        using(var diffHandlerManager = new JsonDiffer.DiffHandlerManager(updatedJson))
        {
            var tasks = differences.AsParallel().Select(diff => TaskManager.RunTask(( ) =>
            {
                var diffHandler = diffHandlerManager.GetDiffHandlerChain();
                diffHandler?.Handle(diff);
                return diffHandler?.DiffResult;
            } , CancellationToken.None)).ToArray();

            Task.WaitAll(tasks , CancellationToken.None);
            tasks.Select(t => t.Result).ToList().ForEach(Console.WriteLine);
            int a = 0;
        }
        int c = 0;
    }
}
