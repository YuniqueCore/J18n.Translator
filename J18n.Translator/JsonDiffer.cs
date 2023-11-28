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

            if(diff is ObjectDiff ObjectDiff)
            {
                // get the path to the object and full update
                // but has Mismateches property, maybe can use it to modify the changed only
                var currentPath = diff.Path.TrimStart('$' , '.');
                var addedProperties = ObjectDiff.Mismatches.Where(added => added.GetType().Equals(typeof(RightOnlyProperty)))
                                                                                        .Select(added => added);
                var removedProperties = ObjectDiff.Mismatches.Where(removed => removed.GetType().Equals(typeof(LeftOnlyProperty)))
                                                                                        .Select(removed => removed);

                for(int i = 0; i < addedProperties.Count(); i++)
                {
                    var propertyName = addedProperties.ElementAt(i).PropertyName;
                    var aa = JsonDiffer.DeserializeByPath(originalJson: jsonSection , propertyName);
                }

                IEnumerable<string> addedPropertiesPath = addedProperties.Select(x => string.Join('.' , currentPath , x.PropertyName));
                IEnumerable<string> removePropertiesPath = removedProperties.Select(x => string.Join('.' , currentPath , x.PropertyName));

                var addedTokensDict = JsonDiffer.DeserializeByPathList(jsonSection , addedPropertiesPath);
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
                for(int i = 0; i < addedItems.Count(); i++)
                {
                    var index = addedItems.ElementAt(i).ItemIndex;
                    var itemPath = string.Join('.' , currentPath , $"[{index}]");
                    var aa = JsonDiffer.DeserializeByPath(jsonSection , itemPath);
                }

                IEnumerable<string> addedPropertiesPath = addedItems.Select(x => string.Join('.' , currentPath , $"[{x.ItemIndex}]"));
                IEnumerable<string> removePropertiesPath = removedItems.Select(x => string.Join('.' , currentPath , $"[{x.ItemIndex}]"));

                var addedTokensDict = JsonDiffer.DeserializeByPathList(jsonSection , addedPropertiesPath);
                // !TODO
                // 1. remove tokens according to removedPropertiesPath
                // 2. add tokens according to addedTokensDict
            }
            else if(diff is ValueDiff valueDiff)
            {
                // get the path of object and only update the value from left to right
            }
            else if(diff is TypeDiff typeDiff)
            {
                //get the path of object, full update.
            }

        }
    }

    public static JToken? DeserializeByPath(string originalJson , string itemPath)
    {
        return DeserializeByPathList(originalJson , new string[] { itemPath }).Values.SingleOrDefault();
    }

    public static Dictionary<string , JToken?> DeserializeByPathList(string originalJson , IEnumerable<string> itemsPath)
    {
        var selectedInstances = new Dictionary<string , JToken?>();
        using(JsonTextReader reader = new JsonTextReader(new StringReader(originalJson)))
        {
            // 移动到指定路径
            while(reader.Read())
            {
                if(itemsPath.Contains(reader.Path))
                {
                    // 在指定路径上开始解析
                    JToken? selectedObject = JToken.ReadFrom(reader);
                    selectedInstances[reader.Path] = selectedObject;
                }
            }
        }
        return selectedInstances;
    }
}