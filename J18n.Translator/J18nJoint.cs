using J18n.Translator.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("J18n.Test")]

namespace J18n.Translator;

public static class J18nJointExtension
{
    /// <summary>
    /// Removes specified sub-joint from the given <see cref="J18nJoint"/> parent.
    /// </summary>
    /// <param name="parent">The parent <see cref="J18nJoint"/> from which to remove sub-joint.</param>
    /// <param name="subPath">An <see cref="IEnumerable{T}"/> of strings representing the paths of sub-joint to be removed.</param>
    /// <returns>An <see cref="J18nJoint"/> object that were removed from the parent.</returns>
    public static J18nJoint? RemoveSubJointByPath(this J18nJoint parent , string subPath)
    {
        return parent.RemoveSubJointsByPaths(new[] { subPath }).FirstOrDefault();
    }

    /// <summary>
    /// Removes specified sub-joints from the given <see cref="J18nJoint"/> parent.
    /// </summary>
    /// <param name="parent">The parent <see cref="J18nJoint"/> from which to remove sub-joints.</param>
    /// <param name="subPaths">An <see cref="IEnumerable{T}"/> of strings representing the paths of sub-joints to be removed.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="J18nJoint"/> objects that were removed from the parent.</returns>
    /// <remarks>
    /// This method iterates through the provided sub-paths, retrieves the corresponding <see cref="J18nJoint"/> objects,
    /// and removes them from the parent. The removed joints are returned in an enumerable collection.
    /// If either the parent or the subPaths parameter is null, an empty collection is returned.
    /// </remarks>
    public static IEnumerable<J18nJoint> RemoveSubJointsByPaths(this J18nJoint parent , IEnumerable<string> subPaths)
    {
        if(parent is null || subPaths is null)
        {
            return Enumerable.Empty<J18nJoint>();
        }

        List<J18nJoint> removedJoints = new List<J18nJoint>();

        foreach(var path in subPaths)
        {
            J18nJoint? j18NJoint = parent.GetSubJointByPath(path.StartsWith('.') ? path : $".{path}");

            if(j18NJoint is not null)
            {
                J18nJoint? removedJoint = null;
                j18NJoint.Parent?.RemoveChild(j18NJoint.Key , out removedJoint);
                removedJoints.Add(removedJoint);
            }
        }

        return removedJoints;
    }

    /// <summary>
    /// Gets a sub-joint within the hierarchy by following the specified sub-joint path.
    /// </summary>
    /// <param name="joint">The root J18nJoint from which to start the search.</param>
    /// <param name="subJointPath">The dot-separated path indicating the location of the desired sub-joint.</param>
    /// <returns>The sub-joint found at the specified path; otherwise, null if the path or sub-joint is not found.</returns>
    /// <exception cref="ArgumentException">Thrown when the sub-joint path does not start with '.'.</exception>
    public static J18nJoint? GetSubJointByPath(this J18nJoint? joint , string subJointPath)
    {
        if(joint is null)
            return null;

        if(!subJointPath.StartsWith('.'))
            throw new ArgumentException(
                $"Sub-Joint Path must start with '.'." ,
                nameof(subJointPath)
            );

        subJointPath = subJointPath.TrimStart('.');

        var pathKeys = ParsePath(subJointPath);

        J18nJoint? subJoint = joint;

        foreach(var key in pathKeys)
        {
            subJoint = subJoint?.Children?.FirstOrDefault(c => c.Key.Equals(key));

            if(subJoint is null)
            {
                // Handle the case when the key is not found
                return null;
            }
        }

        return subJoint;
    }

    /// <summary>
    /// Parses a hierarchical path that may include dot notation ('.') and square brackets ('[]').
    /// </summary>
    /// <param name="path">The path to be parsed.</param>
    /// <returns>An enumerable of individual keys extracted from the path.</returns>

    internal static IEnumerable<string> ParsePath(string path)
    {
        List<string> keys = new List<string>();
        int index = 0;

        while(index < path.Length)
        {
            int openBraketIndex = path.IndexOf('[' , index);
            int dotIndex = path.IndexOf('.' , index);

            if(openBraketIndex == -1 && dotIndex == -1)
            {
                // If no more dots or brackets, add the remaining part of the path as a key
                keys.Add(path.Substring(index));
                break;
            }

            if(openBraketIndex == -1 || (dotIndex != -1 && dotIndex < openBraketIndex))
            {
                // Find the dot for dot notation
                // Extract the key and add it
                keys.Add(path.Substring(index , dotIndex - index));
                index = dotIndex + 1;
            }
            else
            {
                // Find the closing bracket for array index notation
                int closingBracketIndex = path.IndexOf(']' , index);
                if(closingBracketIndex == -1)
                {
                    throw new ArgumentException("Invalid path format: missing ']'." , nameof(path));
                }

                // Extract the array index and add it as a key
                string arrayName = path.Substring(index , openBraketIndex - index);
                string arrayIndex = path.Substring(
                    openBraketIndex ,
                    closingBracketIndex - openBraketIndex + 1
                );
                keys.Add(arrayName);
                keys.Add(arrayIndex);
                index = closingBracketIndex + 1;
            }
        }

        return keys.Where(k => !string.IsNullOrWhiteSpace(k));
    }
}

public enum J18nJointType
{
    Object = 0,
    Array = 1,
    String = 2,
}

public class J18nJoint : ICloneable
{
    /// <summary>
    /// To avoid duplicate keys
    /// </summary>
    private IEnumerable<string>? childrenKeys => this.Children?.Select(c => c._key);

    //private HashSet<int>? childrenIndexes => this.Children?.Select(c => c.Index).ToHashSet();
    private string? _rawText;
    private string _path;
    private string? _comment;
    private string? _description;
    private J18nJointType _type;
    private HashSet<J18nJoint>? _children = null;
    private J18nJoint? _parent;
    private string _key;
    private int _index;

    public string Key
    {
        get { return _key; }
        set
        {
            if(childrenKeys is not null && childrenKeys.Contains(value))
            {
                //throw new DuplicateNameException()
                throw new DuplicateNameException($"Duplicate key found: {value}");
            }

            if(string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(value , "Key value cannot be null or empty");
            }

            if(_key != value)
            {
                _key = value;
                RaiseJointUpdateEvent(this);
            }
        }
    }
    [Newtonsoft.Json.JsonIgnore()]
    public J18nJoint? Parent
    {
        get => _parent;
        private set
        {
            if(_parent != value)
            {
                _parent = value;
                RaiseJointUpdateEvent(this);
            }
        }
    }
    [Newtonsoft.Json.JsonIgnore()]
    public J18nJoint? Last
    {
        get
        {
            J18nJoint? last = null;
            if(this.Index - 1 >= 0)
            {
                last = this.Parent?.GetJoint(this.Index - 1);
            }
            return last;
        }
    }
    [Newtonsoft.Json.JsonIgnore()]
    public J18nJoint? Next
    {
        get
        {
            J18nJoint? next = null;
            if(this.Index + 1 <= this.Parent?.Children?.Count)
            {
                next = this.Parent?.GetJoint(this.Index + 1);
            }
            return next;
        }
    }
    public int Index
    {
        get => _index;
        set
        {
            if(value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Index) ,
                    $"Index should start from 0. Shouldn't a negative number. Please check the value: {value}");
            }

            if(_index != value)
            {
                _index = value;
                RaiseJointUpdateEvent(this);
            }
        }
    }
    public string? RawText
    {
        get => _rawText;
        set
        {
            if(_rawText != value)
            {
                var eventArg = new RawTextExchangArg()
                {
                    Joint = this ,
                    OldRawText = _rawText ,
                    NewRawText = value ,
                };

                _rawText = value;
                RaiseRawTextChangeEvent(eventArg);
                RaiseJointUpdateEvent(this);
            }
        }
    }
    public string Path
    {
        get => GetPath(this);
        private set
        {
            if(_path != value)
            {
                _path = value;
                RaiseJointUpdateEvent(this);
            }
        }
    }
    public string? Comment
    {
        get => _comment;
        set
        {
            if(_comment != value)
            {
                _comment = value;
                RaiseJointUpdateEvent(this);
            }
        }
    }
    public string? Description
    {
        get => _description;
        set
        {
            if(_description != value)
            {
                _description = value;
                RaiseJointUpdateEvent(this);
            }
        }
    }
    [Newtonsoft.Json.JsonIgnore()]
    public DateTime CreationTime { get; private set; } = DateTime.UtcNow;
    [Newtonsoft.Json.JsonIgnore()]
    public DateTime ModificationTime { get; private set; } = DateTime.UtcNow;
    [Newtonsoft.Json.JsonIgnore()]
    public J18nJointType Type
    {
        get => _type;
        set
        {
            if(_type != value)
            {
                _type = value;
                RaiseJointUpdateEvent(this);
            }
        }
    }
    public HashSet<J18nJoint>? Children
    {
        get => _children;
        private set
        {
            _children = value;
            RaiseJointUpdateEvent(this);
            RaiseChildrenUpdateEvent(this);
        }
    }

    public event EventHandler<RawTextExchangArg>? OnRawTextChanging;
    public event EventHandler<J18nJoint>? OnJointUpdating;
    public event EventHandler<J18nJoint>? OnChildrenUpdating;

    public event EventHandler<RawTextExchangArg>? OnRawTextChanged;
    public event EventHandler<J18nJoint>? OnJointUpdated;
    public event EventHandler<J18nJoint>? OnChildrenUpdated;

    protected virtual void RaiseJointUpdateEvent(J18nJoint updatedJoint)
    {

        // 检查是否有订阅者，如果有，则触发事件
        OnJointUpdating?.Invoke(this , updatedJoint);
        UpdateModificationTime(updatedJoint);
        OnJointUpdated?.Invoke(this , updatedJoint);
    }

    protected virtual void RaiseChildrenUpdateEvent(J18nJoint parentJoint)
    {
        OnChildrenUpdating?.Invoke(this , parentJoint);
        ReOrderChildrenIndex(parentJoint);
        OnChildrenUpdated?.Invoke(this , parentJoint);
    }

    protected virtual void RaiseRawTextChangeEvent(RawTextExchangArg textExchangArg)
    {
        OnRawTextChanging?.Invoke(this , textExchangArg);
        UpdateChildrenFromRawText(textExchangArg);
        OnRawTextChanged?.Invoke(this , textExchangArg);
    }

    public J18nJoint( )
    {
        InitEvents();
    }

    public J18nJoint(string key , string rawText , J18nJointType type ,
        string comment = "" , string description = "")
    {
        Key = key;
        RawText = rawText;
        Comment = comment;
        Description = description;
        Type = type;
        InitEvents();
    }

    public J18nJoint(int index , string rawText , J18nJointType type ,
        string comment = "" , string description = "")
    {
        Index = index;
        RawText = rawText;
        Comment = comment;
        Description = description;
        Type = type;
        InitEvents();
    }

    public J18nJoint(int index , string key , string rawText ,
        J18nJointType type , string comment = "" , string description = "")
    {
        Index = index;
        Key = key;
        RawText = rawText;
        Comment = comment;
        Description = description;
        Type = type;
        InitEvents();
    }

    public void InitEvents( )
    {
        //OnJointUpdated += (sender , joint) =>
        //{

        //};

        //OnChildrenUpdated += (sender , joint) =>
        //{

        //};

        //OnRawTextChanged += (sender , arg) =>
        //{

        //};
    }

    private static string GetPath(J18nJoint? joint)
    {
        StringBuilder path = new();
        J18nJoint? current = joint;
        //while(current is not null)
        //{
        //    path.Insert(0 , current.Key + ".");
        //    current = current.Parent;
        //}
        if(joint?.Parent is not null)
        {
            if(joint.Parent.Type.Equals(J18nJointType.Array))
            {
                path.Append($"{joint.Parent.Path}{joint.Key}");
            }
            else
            {
                path.Append(joint.Parent.Path + "." + joint.Key);
            }
        }
        else
        {
            path.Append(joint?.Key);
        }

        return path.ToString().TrimEnd('.');
    }

    public static J18nJointType MapType(JTokenType? jTokenType)
    {
        return jTokenType switch
        {
            JTokenType.Object => J18nJointType.Object,
            JTokenType.Array => J18nJointType.Array,
            JTokenType.String => J18nJointType.String,
            _ => J18nJointType.Object,
        };
    }

    public void ParseRawText(bool recursive = false)
    {
        var root = JsonConvert.DeserializeObject<JToken?>(RawText ?? string.Empty)?.Root;
        UpdateRawTextAndChildren(root , recursive , false);
    }

    public J18nJoint? UpdateRawTextAndChildren(string json , bool recursive = false)
    {
        var root = JsonConvert.DeserializeObject<JToken?>(json)?.Root;
        return UpdateRawTextAndChildren(root , recursive , true);
    }

    public J18nJoint? UpdateRawTextAndChildren(JToken? root ,
        bool recursive = false , bool updateRawText = false)
    {
        if(root is null)
            return null;

        RemoveAllChildren();

        switch(root.Type)
        {
            case JTokenType.String:
                ParseString(root);
                break;
            case JTokenType.Array:
                ParseArray(root , recursive);
                break;
            default:
                //case JTokenType.Object:
                ParseObject(root , recursive);
                break;
        }

        if(updateRawText)
        {
            RawText = root.ToString(Formatting.Indented);
        }

        return this;
    }

    private void ParseString(JToken? root)
    {
        if(root is JProperty str)
        {
            var joint = new J18nJoint()
            {
                Index = 0 ,
                Key = str.Name ,
                RawText = str.Value.ToString(Newtonsoft.Json.Formatting.Indented) ,
                Type = J18nJointType.String ,
            };
            AddChildren(joint);
        }
    }

    private void ParseArray(JToken? root , bool recursive = false)
    {
        var array = root as JArray;
        if(array is not null)
        {
            var children = array
                .Children()
                .Select(
                    (child , index) =>
                    {
                        var type = MapType(child.Type);
                        J18nJoint? joint = null;

                        if(child is JProperty jProperty)
                        {
                            joint = new J18nJoint()
                            {
                                Index = index ,
                                Key = jProperty?.Name ,
                                RawText = jProperty?.Value.ToString(Formatting.Indented) ,
                                Type = MapType(jProperty?.Value.Type) ,
                            };
                            if(recursive)
                            {
                                joint.UpdateRawTextAndChildren(jProperty.Value , recursive);
                            }
                        }
                        else if(child is JValue jValue)
                        {
                            joint = new J18nJoint()
                            {
                                Index = index ,
                                Key = $"[{index}]" ,
                                RawText = jValue?.Value.ToString() ,
                                Type = MapType(jValue?.Type) ,
                            };
                            if(recursive)
                            {
                                joint.UpdateRawTextAndChildren(jValue , recursive);
                            }
                        }
                        else if(child is JObject jObj)
                        {
                            joint = new J18nJoint()
                            {
                                Index = index ,
                                Key = jObj.Path.GetLast() ,
                                RawText = jObj.ToString(Formatting.Indented) ,
                                Type = MapType(jObj?.Type) ,
                            };
                            if(recursive)
                            {
                                joint.UpdateRawTextAndChildren(jObj , recursive);
                            }
                        }
                        else if(child is JArray jArray)
                        {
                            joint = new J18nJoint()
                            {
                                Index = index ,
                                //Key = jArray.Path ,
                                Key = $"[{index}]" ,
                                RawText = jArray.ToString(Formatting.Indented) ,
                                Type = MapType(jArray?.Type) ,
                            };
                            if(recursive)
                            {
                                joint.UpdateRawTextAndChildren(jArray , recursive);
                            }
                        }

                        return joint;
                    }
                );
            AddChildren(children);
        }
    }

    private void ParseObject(JToken? root , bool recursive = false)
    {
        if(root is JObject obj)
        {
            var children = obj.Children()
                .Select(
                    (child , index) =>
                    {
                        JProperty? jProperty = child as JProperty;
                        var joint = new J18nJoint()
                        {
                            Index = index ,
                            Key = jProperty?.Name ,
                            RawText = jProperty?.Value.ToString(Formatting.Indented) ,
                            Type = MapType(jProperty?.Value.Type) ,
                        };
                        if(recursive)
                        {
                            joint.UpdateRawTextAndChildren(jProperty.Value , recursive);
                        }
                        return joint;
                    }
                );
            AddChildren(children);
        }
    }

    private IEnumerable<string> GetDuplicateKeysFromChildren(
        IEnumerable<J18nJoint> children ,
        IEnumerable<string>? ignoreKeys = null
    )
    {
        var duplicateKeys = children
            .GroupBy(joint => joint._key , StringComparer.Ordinal)
            .Where(
                group =>
                    group.Count() > 1 && (ignoreKeys == null || !ignoreKeys.Contains(group.Key))
            )
            .Select(group => group.Key);

        return duplicateKeys;
    }

    private IEnumerable<string> GetDuplicateKeysFromStoredKeys(
        IEnumerable<J18nJoint> children ,
        IEnumerable<string>? ignoreKeys = null
    )
    {
        Func<string , bool> keyFilter = key =>
            (childrenKeys?.Contains(key) == true) && (ignoreKeys?.Contains(key) != true);

        return children.Where(joint => keyFilter(joint._key)).Select(joint => joint._key);
    }

    private void ThrowDuplicatedException(
        IEnumerable<J18nJoint> children ,
        IEnumerable<string>? ignoreKeys = null
    )
    {
        var duplicatedKeysFromChildren = GetDuplicateKeysFromChildren(children , ignoreKeys);
        var duplicatedKeysFromStoredKeys = GetDuplicateKeysFromStoredKeys(children , ignoreKeys);

        if(duplicatedKeysFromChildren.Any() || duplicatedKeysFromStoredKeys.Any())
        {
            var separator = ", ";
            var ignoredKeys = ignoreKeys.Join(separator);
            var duplicatedKeysFromChildenMsg = duplicatedKeysFromChildren.Join(separator);
            var duplicatedKeysFromStoredKeysMsg = duplicatedKeysFromStoredKeys.Join(separator);

            var exceptionMsg =
                $"Ignored keys: {ignoredKeys}"
                + $"\nDuplicate keys found in children: {duplicatedKeysFromChildenMsg}"
                + $"\nDuplicate Keys found from stored children: {duplicatedKeysFromStoredKeysMsg}";

            throw new DuplicateNameException(exceptionMsg);
        }
    }

    private void SetParent(IEnumerable<J18nJoint> children , CancellationToken? ctsToken = null)
    {
        ParallelOptions parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount - 2 ,
            CancellationToken = ctsToken ?? CancellationToken.None
        };

        var result = Parallel.ForEach(
            children ,
            parallelOptions ,
            child =>
            {
                child.Parent = this;
            }
        );

        while(!result.IsCompleted)
        {
            Thread.Sleep(100);
        }
    }

    private async Task SetParentAsync(
        IEnumerable<J18nJoint> children ,
        CancellationToken? ctsToken = null
    )
    {
        var tasks = children.Select(async child =>
        {
            child.Parent = this;
            await Task.Yield(); // 异步操作，确保能够在不同线程上执行
        });

        await Task.WhenAll(tasks);
    }

    private static void UpdateModificationTime(J18nJoint? updatedJoint)
    {

        J18nJoint? current = updatedJoint;
        var modifiedTime = DateTime.UtcNow;

        // If the modification time is within 3 seconds, ignore it.
        if((modifiedTime - current?.ModificationTime) <= TimeSpan.FromSeconds(3))
        {
            return;
        }
        while(current is not null)
        {
            current.ModificationTime = modifiedTime;
            current = current.Parent;
        }
    }

    private static void ReOrderChildrenIndex(J18nJoint parentJoint)
    {
        if(parentJoint.Children is not null && parentJoint.Children.Count > 1)
        {
            if(parentJoint.Children.GroupBy(c => c.Index).Any(group => group.Count() > 1))
            {
                // 1. Sort children by index within the same parent
                var sortedChildren = parentJoint.Children.OrderBy(c => c.Index).ToList();

                // 2. Sort children with the same index by creation time, key, raw text, and type
                var groupedChildren = sortedChildren
                    .GroupBy(c => c.Index)
                    .SelectMany(group =>
                            group.OrderBy(c => c.CreationTime)
                                .ThenBy(c => c.Key)
                                .ThenBy(c => c.RawText)
                                .ThenBy(c => c.Type)
                    )
                    .ToList();

                // 3. Update indexes and set them to the current position
                for(int i = 0; i < groupedChildren.Count; i++)
                {
                    parentJoint.Children.First(c => c.Equals(groupedChildren[i])).Index = i;
                }
            }
        }
    }

    private static void UpdateChildrenFromRawText(RawTextExchangArg textExchangArg)
    {
        if(string.IsNullOrWhiteSpace(textExchangArg.NewRawText))
        {
            textExchangArg.Joint.RemoveAllChildren();
        }
    }

    public void AddChildren(J18nJoint child) => AddChildren(new[] { child });

    public void AddChildren(IEnumerable<J18nJoint> children)
    {
        if(children is null || !children.Any())
        {
            return;
        }
        var listc = new List<J18nJoint>(children);
        //foreach(var item in children)
        //{
        //    Debug.WriteLine(item);
        //}

        ThrowDuplicatedException(children);
        var childrenList = children.ToList();
        SetParent(childrenList);
        Children ??= new();
        Children = Children.Concat(childrenList).ToHashSet();
        RaiseChildrenUpdateEvent(this);
        RaiseJointUpdateEvent(this);
    }

    public async Task AddChildrenAsync(IEnumerable<J18nJoint> children ,
        CancellationToken? ctsToken = null)
    {
        ThrowDuplicatedException(children);
        var childrenList = children.ToList();
        await SetParentAsync(childrenList);
        Children = Children.Concat(childrenList).ToHashSet();
        RaiseChildrenUpdateEvent(this);
        RaiseJointUpdateEvent(this);
    }

    public J18nJoint? GetJoint(Func<J18nJoint , bool> findOldChild)
    {
        return Children?.FirstOrDefault(findOldChild);
    }

    public J18nJoint? GetJoint(string key)
    {
        return GetJoint((c) => c._key.Equals(key , StringComparison.Ordinal));
    }

    public J18nJoint? GetJoint(int index)
    {
        return GetJoint((c) => c.Index.Equals(index));
    }

    /// <summary>
    /// Update the child located by same key name.
    /// </summary>
    /// <param name="newChild"></param>
    /// <param name="oldJoint">the replaced joint</param>
    /// <returns></returns>
    public bool UpdateChild(J18nJoint newChild , out J18nJoint? oldJoint)
    {
        return UpdateChildBase(
            (c) => c._key.Equals(newChild._key) ,
            newChild ,
            FullUpdateAction ,
            out oldJoint
        );
    }

    public bool UpdateChild(int index , J18nJoint newChild , out J18nJoint? oldJoint)
    {
        return UpdateChildBase(
            (c) => c.Index.Equals(index) ,
            newChild ,
            FullUpdateAction ,
            out oldJoint
        );
    }

    public bool UpdateChild(string key , J18nJoint newChild , out J18nJoint? oldJoint)
    {
        return UpdateChildBase(
            (c) => c._key.Equals(key , StringComparison.Ordinal) ,
            newChild ,
            FullUpdateAction ,
            out oldJoint
        );
    }

    public bool UpdateChild(string key , string rawText , J18nJointType type ,
        out J18nJoint? oldJoint , string comment = "" , string description = "")
    {
        var newChild = new J18nJoint(key , rawText , type , comment , description);

        Action<J18nJoint , J18nJoint> PartialUpdateAction = (oldChild , newChild) =>
        {
            oldChild._key = newChild._key;
            oldChild.RawText = newChild.RawText;
            oldChild.Type = newChild.Type;
            oldChild.Comment = newChild.Comment;
            oldChild.Description = newChild.Description;
            //oldChild.ModificationTime = DateTime.UtcNow; // Implemented in root method
        };

        return UpdateChildBase(
            (c) => c._key.Equals(key , StringComparison.Ordinal) ,
            newChild ,
            PartialUpdateAction ,
            out oldJoint
        );
    }

    public bool UpdateChild(int index , string rawText , J18nJointType type ,
        out J18nJoint? oldJoint , string comment = "" , string description = "")
    {
        var newChild = new J18nJoint(index , rawText , type , comment , description);

        Action<J18nJoint , J18nJoint> PartialUpdateAction = (oldChild , newChild) =>
        {
            oldChild.Index = newChild.Index;
            oldChild.RawText = newChild.RawText;
            oldChild.Type = newChild.Type;
            oldChild.Comment = newChild.Comment;
            oldChild.Description = newChild.Description;
            //oldChild.ModificationTime = DateTime.UtcNow; // Implemented in root method
        };

        return UpdateChildBase(
            (c) => c.Index.Equals(index) ,
            newChild ,
            PartialUpdateAction ,
            out oldJoint
        );
    }

    readonly Action<J18nJoint , J18nJoint> FullUpdateAction = (oldChild , newChild) =>
    {
        oldChild.Index = newChild.Index;
        oldChild._key = newChild._key;
        oldChild._rawText = newChild._rawText;
        oldChild.Type = newChild.Type;
        oldChild.Comment = newChild.Comment;
        oldChild.Description = newChild.Description;
        //oldChild.ModificationTime = DateTime.UtcNow; // Implemented in root method
    };

    public bool UpdateChildBase(Func<J18nJoint , bool> findOldChild , J18nJoint newChild ,
        Action<J18nJoint , J18nJoint> updateAction , out J18nJoint? oldJoint)
    {
        var ready2UpdateChild = Children?.FirstOrDefault(findOldChild);
        if(ready2UpdateChild is not null)
        {
            ThrowDuplicatedException(new[] { newChild } , new[] { ready2UpdateChild._key });
            oldJoint = ready2UpdateChild.ShadowClone();
            updateAction.Invoke(ready2UpdateChild , newChild);
            RaiseJointUpdateEvent(ready2UpdateChild);
            if(newChild.Index != oldJoint.Index)
            {
                RaiseChildrenUpdateEvent(this);
            }
            return true;
        }
        oldJoint = null;
        return false;
    }

    public bool RemoveChild(string key , out J18nJoint? removedJoint)
    {
        return RemoveChildBase(
            c => c._key.Equals(key , StringComparison.Ordinal) ,
            out removedJoint
        );
    }

    public bool RemoveChild(int index , out J18nJoint? removedJoint)
    {
        return RemoveChildBase((c) => c.Index == index , out removedJoint);
    }

    public bool RemoveChildBase(Func<J18nJoint , bool> findTheChild , out J18nJoint? removedJoint)
    {
        Children ??= new HashSet<J18nJoint>();

        J18nJoint? ready2removeChild = Children.FirstOrDefault(findTheChild);

        if(ready2removeChild is not null)
        {
            removedJoint = ready2removeChild.ShadowClone();
            Children.Remove(ready2removeChild);

            // Not only need to reallocate the Index
            // But also need to rename the Key for them if parent is Array Type
            Children.Where(j => j.Index > ready2removeChild.Index)
                    .AsParallel()
                    .ForAll(j =>
                    {
                        j.Index--;
                        if(this.Type.Equals(J18nJointType.Array))
                        {
                            j.Key = $"[{j.Index}]";
                        }
                    });

            RaiseJointUpdateEvent(this);
            return true;
        }

        removedJoint = null;
        return false;
    }

    // TODO! Need to Update as the implementation in the RemoveChildBase.
    // And extract the update logic for different joint type out.
    public async Task<(bool, J18nJoint?)> RemoveChildBaseAsync(Func<J18nJoint , bool> findTheChild ,
        CancellationToken ctsToken)
    {
        Children ??= new HashSet<J18nJoint>();

        var removedChild = Children.FirstOrDefault(findTheChild);
        J18nJoint? removedJoint = null;
        if(removedChild is not null)
        {
            removedJoint = removedChild.ShadowClone();
            Children.Remove(removedChild);

            var tasks = Children
                .Where(joint => joint.Index > removedJoint.Index)
                .AsParallel()
                .Select(async j =>
                {
                    await Task.Yield(); // 确保异步任务能够在不同的线程上执行
                    j.Index--;
                    if(this.Type.Equals(J18nJointType.Array))
                    {
                        j.Key = $"[{j.Index}]";
                    }
                });

            await Task.WhenAll(tasks);
            RaiseJointUpdateEvent(this);
            return (true, removedJoint);
        }

        return (false, removedJoint);
    }

    public void RemoveAllChildren( )
    {
        Children?.Clear();
        RaiseJointUpdateEvent(this);
    }

    public J18nJoint ShadowClone(J18nJoint? parent = null)
    {
        return new J18nJoint()
        {
            _parent = parent ,
            _index = this._index ,
            _key = this._key ,
            _rawText = this._rawText , // Set the path for the _rawText field
            _type = this._type ,
            _comment = this._comment ,
            _description = this._description ,
            _path = this._path , // Set the path for the _path field
            CreationTime = this.CreationTime ,
            ModificationTime = this.ModificationTime ,
            _children = this._children?.Select(c => c.ShadowClone(this)).ToHashSet() ,
        };
    }

    public J18nJoint DeepClone(J18nJoint? parent = null , CancellationToken? ctsToken = null)
    {
        var clone = ShadowClone();
        clone._parent = parent;

        ParallelOptions parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount - 2 ,
            CancellationToken = ctsToken ?? CancellationToken.None
        };

        if(clone.Children is not null)
        {
            var result = Parallel.ForEach(clone.Children , parallelOptions ,
                child =>
                {
                    child._parent = clone;
                    child.DeepClone(this , ctsToken);
                }
            );

            while(!result.IsCompleted)
            {
                Thread.Sleep(100);
            }
        }

        return clone;
    }

    public async Task<J18nJoint> DeepCloneAsync(J18nJoint? parent = null , CancellationToken? ctsToken = null)
    {
        var clone = ShadowClone();
        clone._parent = parent;

        ParallelOptions parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount - 2 ,
            CancellationToken = ctsToken ?? CancellationToken.None
        };

        if(clone.Children is not null)
        {
            var result = Parallel.ForEach(
                clone.Children ,
                parallelOptions ,
                child =>
                {
                    child._parent = clone;
                    child.DeepClone(parent , ctsToken);
                }
            );

            while(!result.IsCompleted)
            {
                await Task.Delay(100);
            }
        }

        return clone;
    }

    object ICloneable.Clone( )
    {
        return DeepClone(null , CancellationToken.None);
    }

    public override string ToString( )
    {
        //var settings = new JsonSerializerSettings
        //{
        //    Formatting = Formatting.Indented ,
        //    ReferenceLoopHandling = ReferenceLoopHandling.Ignore ,
        //    NullValueHandling = NullValueHandling.Include
        //};

        //var json = JsonConvert.SerializeObject(this , settings);

        //return json;
        return base.ToString();
    }
    #region EqualityComparer Related

    // override hashcode cause HashSet cannot remove item successfully...
    // The reason in https://stackoverflow.com/questions/26032908/hashset-doesnt-remove-an-object
    // So, forget all about the following implementation
    // Oops, maybe I can wrap it as the following implementation:
    // 1. Implement MyGetHashCode(), MyEquals(object? obj)
    // 2. But this way will cause twice looking up?
    // ```logic
    //      var ready2RemoveItem = Children.Where(j=>j.MyEquals(other) && j.MyGetHashCode().Equals(other.MyGetHashCode());
    //      if (ready2RemoveItem.Count() != 1) throw new MoreThanOneItemFound....
    //      Children.Remove(ready2RemoveItem)
    // ```

    //public override int GetHashCode( )
    //{
    //    int hash = 17;
    //    hash = hash * 23 + Index.GetHashCode();
    //    hash = hash * 23 + (Path?.GetHashCode() ?? 0);
    //    hash = hash * 23 + (Key?.GetHashCode() ?? 0);

    //    return hash;
    //}

    //public override bool Equals(object? obj)
    //{

    //    if(obj == null) return false;

    //    if(ReferenceEquals(this , obj)) return true;

    //    if(obj is J18nJoint other)
    //    {
    //        return this.Index == other.Index
    //        && string.Equals(Path , other.Path , StringComparison.Ordinal)
    //        && string.Equals(Key , other.Key , StringComparison.Ordinal);
    //    }
    //    return false;
    //}

    #endregion
}

public class RawTextExchangArg : EventArgs
{
    public string? OldRawText { get; set; }
    public string? NewRawText { get; set; }

    [NotNull]
    public J18nJoint? Joint { get; set; }

    public RawTextExchangArg( ) { }

    public RawTextExchangArg(string? oldRawText , string? newRawText , J18nJoint? joint)
    {
        OldRawText = oldRawText;
        NewRawText = newRawText;
        Joint = joint;
    }
}
