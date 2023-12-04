using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quibble.CSharp;
using Diff = Quibble.CSharp.Diff;

namespace J18n.Translator;

public class JsonDiffer
{

    public static void ExtractInfo(IReadOnlyList<Diff>? differences , string jsonSection)
    {
        // left = old text, right = new text
        if(differences is null)
        {
            return;
        }

        foreach(var diff in differences)
        {

            // !TODO
            // 1. get the path of the object and the type of the object in parallel way.
            //    Such as Task.WaitAll
            // 2. return the object and the type dictionary, (AddedProperties, RemovedProperties, ModifiedProperties)
            // 3. AddedProperties, add the object to the path
            //    RemovedProperties, remove the object from the path
            //    ModifiedProperties, modify the object from the path

            if(diff is ObjectDiff ObjectDiff)
            {
                // get the path to the object and full update
                // but has Mismateches property, maybe can use it to modify the changed only
                var currentPath = diff.Path.TrimStart('$' , '.');
                var addedProperties = ObjectDiff.Mismatches.Where(added => added.GetType().Equals(typeof(RightOnlyProperty)))
                                                                                        .Select(added => added);
                var removedProperties = ObjectDiff.Mismatches.Where(removed => removed.GetType().Equals(typeof(LeftOnlyProperty)))
                                                                                        .Select(removed => removed);

                IEnumerable<string> addedPropertiesPath = addedProperties.Select(x => string.Join('.' , currentPath , x.PropertyName).Trim('.'));
                IEnumerable<string> removePropertiesPath = removedProperties.Select(x => string.Join('.' , currentPath , x.PropertyName).Trim('.'));

                Dictionary<string , JToken?>? addedTokensDict = JsonDiffer.DeserializeByPath(jsonSection , addedPropertiesPath);
                // !TODO
                // 1. remove tokens according to removedPropertiesPath
                // 2. add tokens according to addedTokensDict
            }
            else if(diff is ArrayDiff arrayDiff)
            {
                // get the path to the object and full update
                // but has Mismateches property, maybe can use it to modify the changed only
                var currentPath = diff.Path.TrimStart('$' , '.');
                var addedItems = arrayDiff.Mismatches.Where(added => added.GetType().Equals(typeof(RightOnlyItem)))
                                                                                        .Select(added => added);
                var removedItems = arrayDiff.Mismatches.Where(removed => removed.GetType().Equals(typeof(LeftOnlyItem)))
                                                                                        .Select(removed => removed);

                IEnumerable<string> addedPropertiesPath = addedItems.Select((x , index) => $"{currentPath}[{index}]".Trim('.'));
                IEnumerable<string> removePropertiesPath = removedItems.Select((x , index) => $"{currentPath}[{index}]".Trim('.'));

                Dictionary<string , JToken?>? addedTokensDict = JsonDiffer.DeserializeByPath(jsonSection , addedPropertiesPath);
                // !TODO
                // 1. remove tokens according to removedPropertiesPath
                // 2. add tokens according to addedTokensDict
            }
            else if(diff is ValueDiff valueDiff)
            {
                // get the path of object and only update the value from left to right
                var currentPath = diff.Path.TrimStart('$' , '.');
                Dictionary<string , JToken?>? updatedTokensDict = JsonDiffer.DeserializeByPath(jsonSection , new string[] { currentPath });
            }
            else if(diff is TypeDiff typeDiff)
            {
                //get the path of object, full update.
                var currentPath = diff.Path.TrimStart('$' , '.');
                Dictionary<string , JToken?>? updatedTokensDict = JsonDiffer.DeserializeByPath(jsonSection , new string[] { currentPath });
            }

        }
    }

    public static JToken? DeserializeByPath(string originalJson , string itemPath)
    {
        return DeserializeByPath(originalJson , new string[] { itemPath }).Values.SingleOrDefault();
    }

    public static Dictionary<string , JToken?> DeserializeByPath(string originalJson , IEnumerable<string> itemsPath)
    {
        var selectedInstances = new Dictionary<string , JToken?>();
        using(JsonTextReader reader = new JsonTextReader(new StringReader(originalJson)))
        {
            // 移动到指定路径
            while(reader.Read())
            {
                if(selectedInstances.Count >= itemsPath.Count())
                {
                    break;
                }

                if(itemsPath.Contains(reader.Path))
                {
                    // 在指定路径上开始解析
                    var currentPath = reader.Path;
                    JToken? selectedObject = JToken.ReadFrom(reader);
                    selectedInstances[currentPath] = selectedObject;
                }
            }
        }
        return selectedInstances;
    }

}