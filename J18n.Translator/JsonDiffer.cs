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

        public IDiffHandler? CreateDiffHandler(DiffType diffType)
        {
            BaseDifferHandler? handler = diffType switch
            {
                DiffType.ObjectDiff => new ObjectDiffHandler(),
                DiffType.ArrayDiff => new ArrayDiffHandler(),
                DiffType.ValueDiff => new ValueDiffHandler(),
                DiffType.TypeDiff => new TypeDiffHandler(),
                _ => null,
            };
            DiffHandlers?.Add(handler);
            return handler;
        }

        public IDiffHandler? GetDiffHandlerChain( )
        {
            var jsonD = new JsonDiffHandler();
            var objD = new ObjectDiffHandler();
            var arrayD = new ArrayDiffHandler();
            var valueD = new ValueDiffHandler();
            var typeD = new TypeDiffHandler();

            jsonD.SetNext(objD).SetNext(arrayD)
                .SetNext(valueD).SetNext(typeD);

            DiffHandlers?.Add(jsonD);
            return jsonD;
        }

        public void Dispose( )
        {
            BaseDifferHandler.JsonSection = null;
            DiffHandlers = null;
        }
    }

    public enum DiffType
    {
        ObjectDiff,
        ArrayDiff,
        ValueDiff,
        TypeDiff
    }

    public class DiffResult
    {
        public DiffType DiffType { get; set; }
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
        IDiffHandler? Next { get; }
        DiffResult? DiffResult { get; }
        static string? JsonSection { get; set; }
        bool HandledOver { get; }
        IDiffHandler SetNext(IDiffHandler next);
        DiffResult? Handle(Diff diff);
    }

    protected abstract class BaseDifferHandler : IDiffHandler
    {
        private BaseDifferHandler? _next;
        protected Lazy<DiffResult?>? _diffResult { get; private set; } = new Lazy<DiffResult?>(( ) => new DiffResult() , LazyThreadSafetyMode.ExecutionAndPublication);

        public static string? JsonSection { get; set; }
        public DiffResult? DiffResult => _diffResult?.Value;
        public bool HandledOver { get; protected set; } = false;
        public IDiffHandler? Next => _next;
        public IDiffHandler SetNext(IDiffHandler next)
        {
            if(next is null)
                throw new ArgumentNullException(nameof(next) , "The next is null");

            _next = (BaseDifferHandler?)next;
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

        private void RestoreDiffResult( )
        {
            _diffResult = null; // restore the diffResult to null
            var nextHandler = _next;
            while(nextHandler is not null)
            {
                nextHandler._diffResult = null; // restore the diffResult to null
                nextHandler = nextHandler._next;
            }
        }

        private void RaiseHandledOverStateChanged( )
        {
            if(HandledOver)
            {
                var nextHandler = _next;
                while(nextHandler is not null)
                {
                    nextHandler.HandledOver = true;
                    nextHandler._diffResult = _diffResult; // pass the diffResult to next handler
                    nextHandler = nextHandler._next;
                }
            }
            else
            {
                _diffResult = null;
            }
        }

        protected virtual void SetDiffResult(DiffType diffType , Dictionary<string , JToken?>? addedPropertiesDict , IEnumerable<string>? removedPaths)
        {
            DiffResult!.DiffType = diffType;
            DiffResult.UpdatedPropertiesDict = addedPropertiesDict;
            DiffResult.RemovedPropertiesPath = removedPaths;
        }

        public DiffResult? Handle(Diff diff)
        {
            var parentResult = DiffResult;
            if(HandledOver)
            {
                RestoreDiffResult();
                return parentResult;
            }
            DiffInternalHandle(diff);
            RaiseHandledOverStateChanged();
            return _next is null ? DiffResult : _next.Handle(diff);
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
                clonedHandler._next = (BaseDifferHandler?)nextCloneable.Clone();
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

    private class JsonDiffHandler : BaseDifferHandler
    {
        protected override void DiffInternalHandle(Diff unKnownDiff)
        {
            ThrowNullException(unKnownDiff); // Check it once only
        }
    }

    private class ObjectDiffHandler : BaseDifferHandler
    {
        protected override void DiffInternalHandle(Diff unKnownDiff)
        {
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

                SetDiffResult(DiffType.ObjectDiff , addedTokensDict , removePropertiesPath);
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

                SetDiffResult(DiffType.ArrayDiff , addedTokensDict , removePropertiesPath);
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

                SetDiffResult(DiffType.ValueDiff , updatedTokensDict , null);
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

                SetDiffResult(DiffType.TypeDiff , updatedTokensDict , null);
                HandledOver = true;
            }
        }
    }

    /// <summary>
    /// Deserialize Json string from specified path
    /// </summary>
    /// <param name="originalJson"></param>
    /// <param name="itemPath">Item path</param>
    /// <returns>JToken?</returns>
    /// <exception cref="ArgumentNullException">The originalJson is null</exception>"
    public static JToken? DeserializeByPath(string originalJson , string itemPath)
    {
        return DeserializeByPath(originalJson , new string[] { itemPath }).Values.SingleOrDefault();
    }

    /// <summary>
    /// Deserialize Json string from specified path List
    /// </summary>
    /// <param name="originalJson"></param>
    /// <param name="itemsPath">Item path list</param>
    /// <returns>Dictionary[Item_path , JToken?]</returns>
    /// <exception cref="ArgumentNullException">The originalJson is null</exception>
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