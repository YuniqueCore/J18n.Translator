﻿using J18n.Test.TestData.Json;
using J18n.Translator;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

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
    public void JsonDiffResultsTest(string originalJson , string updatedJson)
    {
        var differences = Quibble.CSharp.JsonStrings.Diff(originalJson , updatedJson);

        using(var diffHandlerManager = new JsonDiffer.DiffHandlerManager(updatedJson))
        {
            List<JsonDiffer.DiffResult?> diffResults = new List<JsonDiffer.DiffResult?>();
            //for(int i = 0; i < differences.Count; i++)
            //{
            //    var diffHandler = diffHandlerManager.GetDiffHandlerChain();
            //    var rst = diffHandler?.Handle(differences[i]);
            //    diffResults.Add(rst);
            //    Debug.WriteLine(rst);
            //}

            var tasks = differences.AsParallel().Select(diff => TaskManager.RunTask(( ) =>
            {
                var diffHandler = diffHandlerManager.GetDiffHandlerChain();
                var rst = diffHandler?.Handle(diff);
                diffResults.Add(rst);
                return rst;
            } , CancellationToken.None)).ToArray();

            Task.WaitAll(tasks);
            tasks.Select(t => t.Result).ToList().ForEach((diffResult) =>
            {
                Debug.WriteLine(diffResult);
            });

            // The diff result count must be equal to the differences count
            Assert.AreEqual(differences.Count , diffResults.Count);

            // When the diff result is not null, the other diffs result must be null
            Assert.IsTrue(diffHandlerManager!.DiffHandlers!
                .Where(handler =>
                {
                    return handler!.Next!.DiffResult is not null;
                })
                .All(handler =>
                {
                    var IsNull = true;
                    var next = handler?.Next!.Next;
                    while(next is not null)
                    {
                        IsNull &= next.DiffResult is null;
                        next = next.Next;
                    }
                    return IsNull;
                }));

            // The diffresult is not null count + the null count must be equal to the diff handler chain length in one chain
            var chain = diffHandlerManager.GetDiffHandlerChain();
            int expectedLength = 0;
            var next = chain;
            while(next is not null)
            {
                expectedLength++;
                next = next.Next;
            }

            for(int i = 0; i < diffHandlerManager!.DiffHandlers!.Count; i++)
            {
                // The diffresult is not null count + the null count must be equal to the diff handler chain length in one chain
                var diffHandler = diffHandlerManager!.DiffHandlers![i];
                int nullCount = 0, notNullCount = 0;
                var nextHandler = diffHandler;
                while(nextHandler is not null)
                {
                    _ = nextHandler.DiffResult is null ? nullCount++ : notNullCount++;
                    nextHandler = nextHandler!.Next;
                }
                Assert.AreEqual(expectedLength , nullCount + notNullCount);
            }
        }
    }


    [TestMethod]
    [DataRow(JsonDiffData.initialJson , JsonDiffData.updatedJson , JsonDiffer.DiffType.ObjectDiff)]
    [DataRow(JsonDiffData.initialJson , JsonDiffData.updatedJson , JsonDiffer.DiffType.ArrayDiff)]
    [DataRow(JsonDiffData.initialJson , JsonDiffData.updatedJson , JsonDiffer.DiffType.ValueDiff)]
    [DataRow(JsonDiffData.initialJson , JsonDiffData.updatedJson , JsonDiffer.DiffType.TypeDiff)]
    public void DiffTypeUnitTest(string originalJson , string updatedJson , JsonDiffer.DiffType type)
    {
        var differences = Quibble.CSharp.JsonStrings.Diff(originalJson , updatedJson);

        using(var diffHandlerManager = new JsonDiffer.DiffHandlerManager(updatedJson))
        {

            for(int i = 0; i < differences.Count; i++)
            {
                var valueDiffHandler = diffHandlerManager.CreateDiffHandler(type);

                var diff = differences[i];
                var result = valueDiffHandler?.Handle(diff);
                if(result?.DiffType == type)
                {
                    Assert.IsNull((type.Equals(JsonDiffer.DiffType.ValueDiff) || type.Equals(JsonDiffer.DiffType.TypeDiff))
                                            ? result.RemovedPropertiesPath : null);
                    Assert.IsNotNull(result.UpdatedPropertiesDict);
                }
                Debug.WriteLine(result);
            }

        }
    }



    [TestMethod]
    [DataRow(JsonDiffData.initialJson , JsonDiffData.updatedJson)]
    public void DiffRemovedPropertiesOnJTokenUnitTest(string originalJson , string updatedJson)
    {
        // According to the removed properites path to delete the properties in the original json
        // 1. get the removed properties path
        // 2. remove the properties in the original JToken
        // Some potantial problems:
        // 1. the removed properties path is not in the original json
        var originalJToken = JToken.Parse(originalJson).Root;
        var differences = Quibble.CSharp.JsonStrings.Diff(originalJson , updatedJson);

        using(var diffHandlerManager = new JsonDiffer.DiffHandlerManager(updatedJson))
        {
            var results = new List<JsonDiffer.DiffResult?>();
            for(int i = 0; i < differences.Count; i++)
            {
                var valueDiffHandler = diffHandlerManager.GetDiffHandlerChain();

                var diff = differences[i];
                var result = valueDiffHandler?.Handle(diff);
                results.Add(result);
            }
            var removedProperties = results.Select(r => r?.RemovedPropertiesPath)
                .Where(p => p is not null)
                .SelectMany(p => p!)
                .Distinct();
            Assert.IsNotNull(removedProperties);
            Assert.IsTrue(removedProperties.Any());
            Assert.IsTrue(originalJToken.SelectToken("menuItems2[0]")?.Value<string>()?.Equals("Services") ,
                "The first item in array is Services currently.");

            JsonDiffer.RemoveProperties(originalJToken , removedProperties);

            Assert.IsTrue(originalJToken.SelectToken("menuItems2[0]")?.Value<string>()?.Equals("Home") ,
                "The first item in array is Home now. The previous one was deleted successfully.");

        }
    }




}
