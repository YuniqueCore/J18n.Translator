using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quibble.CSharp;
using System.Text;
using ArrayDiff = Quibble.CSharp.ArrayDiff;
using Diff = Quibble.CSharp.Diff;
using ObjectDiff = Quibble.CSharp.ObjectDiff;
using TypeDiff = Quibble.CSharp.TypeDiff;
using ValueDiff = Quibble.CSharp.ValueDiff;

namespace J18n.Translator;

public class JsonDiffer
{
    public class DiffHandlerManager : IDisposable
    {
        public DiffHandlerManager(string jsonSection)
        {
            if(jsonSection is null)
            {
                throw new ArgumentNullException(nameof(jsonSection) , "JsonSection is null. The Updated JSON string is required.");
            }
            BaseDifferHandler.JsonSection = jsonSection;
            DiffHandlers = [];
        }

        public List<IDiffHandler?>? DiffHandlers { get; private set; }

        public IDiffHandler? GetDiffHandlerChain( )
        {
            var objD = new ObjectDiffHandler();
            var arrayD = new ArrayDiffHandler();
            var valueD = new ValueDiffHandler();
            var typeD = new TypeDiffHandler();


            objD.SetNext(arrayD)
                .SetNext(valueD)
                .SetNext(typeD);

            DiffHandlers?.Add(objD);
            return objD;
        }

        public void Dispose( )
        {
            BaseDifferHandler.JsonSection = null;
            DiffHandlers = null;
        }
    }

    public class DiffResult
    {
        public Dictionary<string , JToken?>? UpdatedPropertiesDict { get; set; }
        public IEnumerable<string>? RemovedPropertiesPath { get; set; }
        public override string ToString( )
        {
            var sb = new StringBuilder();
            if(UpdatedPropertiesDict is not null)
            {
                sb.AppendLine("UpdatedProperties:");
                for(int i = 0; i < UpdatedPropertiesDict.Count; i++)
                {
                    var kv = UpdatedPropertiesDict.ElementAt(i);
                    sb.AppendLine($"\t {i}. {kv.Key}: {kv.Value}");
                }
            }
            if(RemovedPropertiesPath is not null)
            {
                sb.AppendLine("RemovedProperties:");
                for(int i = 0; i < RemovedPropertiesPath.Count(); i++)
                {
                    var path = RemovedPropertiesPath.ElementAt(i);
                    sb.AppendLine($"\t {i}. {path}");
                }
            }
            return sb.ToString();
        }
    }

    public interface IDiffHandler : IDisposable, ICloneable
    {
        DiffResult? DiffResult { get; }
        static string? JsonSection { get; set; }
        bool HandledOver { get; }
        IDiffHandler SetNext(IDiffHandler next);
        void Handle(Diff diff);
    }

    protected abstract class BaseDifferHandler : IDiffHandler
    {
        private IDiffHandler? _next;
        protected Lazy<DiffResult?>? _diffResult { get; private set; } = new Lazy<DiffResult?>(( ) => new DiffResult());
        public static string? JsonSection { get; set; }
        public DiffResult? DiffResult => _diffResult?.Value;
        public bool HandledOver { get; protected set; } = false;

        public IDiffHandler SetNext(IDiffHandler next)
        {
            if(next is null)
                throw new ArgumentNullException(nameof(next) , "The next is null");

            _next = next;
            return next;
        }

        public static void ThrowNullException(Diff diff)
        {
            if(diff is null)
            {
                throw new ArgumentNullException(nameof(diff) , "The Diff is null");
            }
        }

        protected abstract void DiffInternalHandle(Diff unKnownDiff);

        protected virtual void SetDiffResult(Dictionary<string , JToken?>? addedPropertiesDict , IEnumerable<string>? removedPaths)
        {
            DiffResult.UpdatedPropertiesDict = addedPropertiesDict;
            DiffResult.RemovedPropertiesPath = removedPaths;
        }

        public void Handle(Diff diff)
        {
            DiffInternalHandle(diff);
            if(HandledOver)
                return;
            _next?.Handle(diff);
        }

        public void Dispose( )
        {
            // Implement IDisposable if needed for resource cleanup
            _diffResult = null;
            _next = null;
            JsonSection = null;
        }

        public object Clone( )
        {
            BaseDifferHandler clonedHandler = (BaseDifferHandler)MemberwiseClone();

            // 如果 _next 是一个类的实例，而不仅仅是接口
            if(_next is not null && _next is ICloneable nextCloneable)
            {
                clonedHandler._next = (IDiffHandler)nextCloneable.Clone();
            }

            if(_diffResult != null)
            {
                clonedHandler._diffResult = new Lazy<DiffResult?>(( ) => new DiffResult
                {
                    UpdatedPropertiesDict = _diffResult.Value?.UpdatedPropertiesDict?
                                            .ToDictionary(entry => entry.Key ,
                                                        entry => entry.Value) ,
                    RemovedPropertiesPath = _diffResult.Value?.RemovedPropertiesPath?.ToList()
                });
            }

            return clonedHandler;
        }
    }

    private class ObjectDiffHandler : BaseDifferHandler
    {
        protected override void DiffInternalHandle(Diff unKnownDiff)
        {
            ThrowNullException(unKnownDiff); // Check it once only
            if(unKnownDiff is ObjectDiff diff)
            {
                var currentPath = diff.Path.TrimStart('$' , '.');
                var addedProperties = diff.Mismatches.Where(added => added.GetType().Equals(typeof(RightOnlyProperty)))
                                                                                        .Select(added => added);
                var removedProperties = diff.Mismatches.Where(removed => removed.GetType().Equals(typeof(LeftOnlyProperty)))
                                                                                        .Select(removed => removed);

                IEnumerable<string> addedPropertiesPath = addedProperties.Select(x => string.Join('.' , currentPath , x.PropertyName).Trim('.'));
                IEnumerable<string> removePropertiesPath = removedProperties.Select(x => string.Join('.' , currentPath , x.PropertyName).Trim('.'));

                Dictionary<string , JToken?>? addedTokensDict = JsonDiffer.DeserializeByPath(JsonSection! , addedPropertiesPath);

                SetDiffResult(addedTokensDict , removePropertiesPath);
                HandledOver = true;
            }
        }
    }

    private class ArrayDiffHandler : BaseDifferHandler
    {
        protected override void DiffInternalHandle(Diff unKnownDiff)
        {
            if(unKnownDiff is ArrayDiff diff)
            {
                var currentPath = diff.Path.TrimStart('$' , '.');
                var addedItems = diff.Mismatches.Where(added => added.GetType().Equals(typeof(RightOnlyItem)))
                                                                                        .Select(added => added);
                var removedItems = diff.Mismatches.Where(removed => removed.GetType().Equals(typeof(LeftOnlyItem)))
                                                                                        .Select(removed => removed);

                IEnumerable<string> addedPropertiesPath = addedItems.Select((x , index) => $"{currentPath}[{index}]".Trim('.'));
                IEnumerable<string> removePropertiesPath = removedItems.Select((x , index) => $"{currentPath}[{index}]".Trim('.'));

                Dictionary<string , JToken?>? addedTokensDict = JsonDiffer.DeserializeByPath(JsonSection! , addedPropertiesPath);

                SetDiffResult(addedTokensDict , removePropertiesPath);
                HandledOver = true;
            }
        }
    }

    private class ValueDiffHandler : BaseDifferHandler
    {
        protected override void DiffInternalHandle(Diff unKnownDiff)
        {
            if(unKnownDiff is ValueDiff diff)
            {
                var currentPath = diff.Path.TrimStart('$' , '.');
                Dictionary<string , JToken?>? updatedTokensDict = JsonDiffer.DeserializeByPath(JsonSection! , new string[] { currentPath });

                SetDiffResult(updatedTokensDict , null);
                HandledOver = true;
            }
        }
    }

    private class TypeDiffHandler : BaseDifferHandler
    {
        protected override void DiffInternalHandle(Diff unKnownDiff)
        {
            if(unKnownDiff is TypeDiff diff)
            {
                var currentPath = diff.Path.TrimStart('$' , '.');
                Dictionary<string , JToken?>? updatedTokensDict = JsonDiffer.DeserializeByPath(JsonSection! , new string[] { currentPath });

                SetDiffResult(updatedTokensDict , null);
                HandledOver = true;
            }
        }
    }



    public static void ExtractInfo(IReadOnlyList<Diff>? differences , string jsonSection)
    {
        // left = old text, right = new text
        if(differences is null)
        {
            return;
        }

        Dictionary<string , JToken?>? updatedPropertiesDict = null;
        IEnumerable<string>? removedPropertiesPath = null;

        List<Task> extractInfoTasks = new List<Task>();

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