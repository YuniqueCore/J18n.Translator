using J18n.Translator.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace J18n.Translator;

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
#pragma warning disable IDE1006 // 命名样式
    private string _key { get; set; }
#pragma warning restore IDE1006 // 命名样式
    [NotNull]
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

            _key = value;
        }
    }
    public J18nJoint? Parent { get; private set; }
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
    public J18nJoint? Next
    {
        get
        {
            J18nJoint? next = null;
            if(this.Index + 1 <= this.Parent?.Children.Count)
            {
                next = this.Parent?.GetJoint(this.Index + 1);
            }
            return next;
        }
    }
    public int Index { get; set; }
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
                RaiseRawTextChangedEvent(eventArg);
            }
        }
    }

    public string Path
    {
        get
        {
            if(string.IsNullOrWhiteSpace(_path))
            {
                _path = GetPath(this);
            }
            return _path;
        }

        private set => _path = value;
    }
    public string? Comment { get; set; }
    public string? Description { get; set; }
    public DateTime CreationTime { get; private set; } = DateTime.UtcNow;
    public DateTime ModificationTime { get; private set; } = DateTime.UtcNow;
    public J18nJointType Type { get; set; }
    [NotNull] public HashSet<J18nJoint>? Children { get; private set; } = null;

    public event EventHandler<RawTextExchangArg>? OnRawTextChanged;
    public event EventHandler<J18nJoint>? OnJointUpdated;
    public event EventHandler<J18nJoint>? OnChildrenUpdated;

    protected virtual void RaiseJointUpdatedEvent(J18nJoint updatedJoint)
    {
        // 检查是否有订阅者，如果有，则触发事件
        OnJointUpdated?.Invoke(this , updatedJoint);
    }
    protected virtual void RaiseChildrenUpdatedEvent(J18nJoint parentJoint)
    {
        OnChildrenUpdated?.Invoke(this , parentJoint);
    }
    protected virtual void RaiseRawTextChangedEvent(RawTextExchangArg textExchangArg)
    {
        OnRawTextChanged?.Invoke(this , textExchangArg);
    }

    public J18nJoint( ) { InitEvents(); }

    public J18nJoint(string key , string rawText , J18nJointType type , string comment = "" , string description = "")
    {
        Key = key;
        RawText = rawText;
        Comment = comment;
        Description = description;
        Type = type;
        InitEvents();
    }

    public J18nJoint(int index , string rawText , J18nJointType type , string comment = "" , string description = "")
    {
        Index = index;
        RawText = rawText;
        Comment = comment;
        Description = description;
        Type = type;
        InitEvents();
    }

    public J18nJoint(int index , string key , string rawText , J18nJointType type , string comment = "" , string description = "")
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
        OnJointUpdated += (sender , joint) =>
        {
            UpdateModificationTime(joint);
        };

        OnChildrenUpdated += (sender , joint) =>
        {
            ReOrderChildrenIndex(joint);
        };

        OnRawTextChanged += (sender , arg) =>
        {
            UpdateChildrenFromRawText(arg);
        };
    }

    private static string GetPath(J18nJoint? joint)
    {
        StringBuilder path = new();
        J18nJoint? current = joint;
        while(current is not null)
        {
            path.Insert(0 , current.Key + ".");
            current = current.Parent;
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
        ParseRawTextToChildren(root , recursive);
    }

    public J18nJoint? ParseRawTextToChildren(string json , bool recursive = false)
    {
        var root = JsonConvert.DeserializeObject<JToken?>(json)?.Root;
        return ParseRawTextToChildren(root , recursive);
    }

    public J18nJoint? ParseRawTextToChildren(JToken? root , bool recursive = false)
    {
        if(root is null) return null;

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
            var children = array.Children().Select((child , index) =>
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
                        joint.ParseRawTextToChildren(jProperty.Value);
                    }
                }
                else if(child is JValue jValue)
                {
                    joint = new J18nJoint()
                    {
                        Index = index ,
                        Key = index.ToString() ,
                        RawText = jValue?.Value.ToString() ,
                        Type = MapType(jValue?.Type) ,
                    };
                    if(recursive)
                    {
                        joint.ParseRawTextToChildren(jValue);
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
                        joint.ParseRawTextToChildren(jObj);
                    }
                }

                return joint;
            });
            AddChildren(children);
        }
    }

    private void ParseObject(JToken? root , bool recursive = false)
    {
        if(root is JObject obj)
        {
            var children = obj.Children().Select((child , index) =>
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
                    joint.ParseRawTextToChildren(jProperty.Value);
                }
                return joint;
            });
            AddChildren(children);
        }
    }

    private IEnumerable<string> GetDuplicateKeysFromChildren(IEnumerable<J18nJoint> children , IEnumerable<string>? ignoreKeys = null)
    {
        var duplicateKeys = children
            .GroupBy(joint => joint._key , StringComparer.Ordinal)
            .Where(group => group.Count() > 1 && (ignoreKeys == null || !ignoreKeys.Contains(group.Key)))
            .Select(group => group.Key);

        return duplicateKeys;
    }

    private IEnumerable<string> GetDuplicateKeysFromStoredKeys(IEnumerable<J18nJoint> children , IEnumerable<string>? ignoreKeys = null)
    {
        Func<string , bool> keyFilter = key =>
            (childrenKeys?.Contains(key) == true) && (ignoreKeys?.Contains(key) != true);

        return children
            .Where(joint => keyFilter(joint._key))
            .Select(joint => joint._key);
    }

    private void ThrowDuplicatedException(IEnumerable<J18nJoint> children , IEnumerable<string>? ignoreKeys = null)
    {
        var duplicatedKeysFromChildren = GetDuplicateKeysFromChildren(children , ignoreKeys);
        var duplicatedKeysFromStoredKeys = GetDuplicateKeysFromStoredKeys(children , ignoreKeys);

        if(duplicatedKeysFromChildren.Any() || duplicatedKeysFromStoredKeys.Any())
        {
            var separator = ", ";
            var ignoredKeys = ignoreKeys.Join(separator);
            var duplicatedKeysFromChildenMsg = duplicatedKeysFromChildren.Join(separator);
            var duplicatedKeysFromStoredKeysMsg = duplicatedKeysFromStoredKeys.Join(separator);

            var exceptionMsg = $"Ignored keys: {ignoredKeys}" +
                $"\nDuplicate keys found in children: {duplicatedKeysFromChildenMsg}" +
                $"\nDuplicate Keys found from stored children: {duplicatedKeysFromStoredKeysMsg}";

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

        var result = Parallel.ForEach(children , parallelOptions , child =>
        {
            child.Parent = this;
        });

        while(!result.IsCompleted)
        {
            Thread.Sleep(100);
        }
    }

    private async Task SetParentAsync(IEnumerable<J18nJoint> children , CancellationToken? ctsToken = null)
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
            if(parentJoint.Children.GroupBy(c => c.Index)
                                .Any(group => group.Count() > 1))
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
                                    .ThenBy(c => c.Type))
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
        ThrowDuplicatedException(children);
        var childrenList = children.ToList();
        SetParent(childrenList);
        Children ??= new();
        Children = Children.Concat(childrenList).ToHashSet();
        RaiseChildrenUpdatedEvent(this);
        RaiseJointUpdatedEvent(this);
    }

    public async Task AddChildrenAsync(IEnumerable<J18nJoint> children , CancellationToken? ctsToken = null)
    {
        ThrowDuplicatedException(children);
        var childrenList = children.ToList();
        await SetParentAsync(childrenList);
        Children = Children.Concat(childrenList).ToHashSet();
        RaiseChildrenUpdatedEvent(this);
        RaiseJointUpdatedEvent(this);
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
        return UpdateChildBase((c) => c._key.Equals(newChild._key) , newChild , FullUpdateAction , out oldJoint);
    }

    public bool UpdateChild(int index , J18nJoint newChild , out J18nJoint? oldJoint)
    {
        return UpdateChildBase((c) => c.Index.Equals(index) , newChild , FullUpdateAction , out oldJoint);
    }

    public bool UpdateChild(string key , J18nJoint newChild , out J18nJoint? oldJoint)
    {
        return UpdateChildBase((c) => c._key.Equals(key , StringComparison.Ordinal) , newChild , FullUpdateAction , out oldJoint);
    }

    public bool UpdateChild(string key , string rawText , J18nJointType type , out J18nJoint? oldJoint , string comment = "" , string description = "")
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

        return UpdateChildBase((c) => c._key.Equals(key , StringComparison.Ordinal) , newChild , PartialUpdateAction , out oldJoint);

    }

    public bool UpdateChild(int index , string rawText , J18nJointType type , out J18nJoint? oldJoint , string comment = "" , string description = "")
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

        return UpdateChildBase((c) => c.Index.Equals(index) , newChild , PartialUpdateAction , out oldJoint);
    }

    readonly Action<J18nJoint , J18nJoint> FullUpdateAction = (oldChild , newChild) =>
    {
        oldChild.Index = newChild.Index;
        oldChild._key = newChild._key;
        oldChild.RawText = newChild.RawText;
        oldChild.Type = newChild.Type;
        oldChild.Comment = newChild.Comment;
        oldChild.Description = newChild.Description;
        //oldChild.ModificationTime = DateTime.UtcNow; // Implemented in root method
    };

    public bool UpdateChildBase(Func<J18nJoint , bool> findOldChild , J18nJoint newChild , Action<J18nJoint , J18nJoint> updateAction , out J18nJoint? oldJoint)
    {
        var oldChild = Children?.FirstOrDefault(findOldChild);
        if(oldChild is not null)
        {
            ThrowDuplicatedException(new[] { newChild } , new[] { oldChild._key });
            oldJoint = oldChild.ShadowClone();
            updateAction.Invoke(oldChild , newChild);
            RaiseJointUpdatedEvent(oldChild);
            if(newChild.Index != oldChild.Index)
            {
                RaiseChildrenUpdatedEvent(this);
            }
            return true;
        }
        oldJoint = null;
        return false;
    }

    public bool RemoveChild(string key , out J18nJoint? removedJoint)
    {
        return RemoveChildBase((c) => c._key.Equals(key , StringComparison.Ordinal) , out removedJoint);
    }

    public bool RemoveChild(int index , out J18nJoint? removedJoint)
    {
        return RemoveChildBase((c) => c.Index == index , out removedJoint);
    }

    public bool RemoveChildBase(Func<J18nJoint , bool> findTheChild , out J18nJoint? removedJoint)
    {
        Children ??= new HashSet<J18nJoint>();

        var removedChild = Children.FirstOrDefault(findTheChild);

        if(removedChild is not null)
        {
            removedJoint = removedChild.ShadowClone();
            Children.Remove(removedChild);
            foreach(var joint in Children)
            {
                if(joint.Index < removedJoint.Index)
                {
                    continue;
                }
                else
                {
                    joint.Index--;
                }
            }
            RaiseJointUpdatedEvent(this);
            return true;
        }

        removedJoint = null;
        return false;
    }

    public async Task<(bool, J18nJoint?)> RemoveChildBaseAsync(Func<J18nJoint , bool> findTheChild , CancellationToken ctsToken)
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
                .Select(async joint =>
                {
                    joint.Index--;
                    await Task.Yield(); // 确保异步任务能够在不同的线程上执行
                });
            RaiseJointUpdatedEvent(this);
            await Task.WhenAll(tasks);
            return (true, removedJoint);
        }

        return (false, removedJoint);
    }

    public void RemoveAllChildren( )
    {
        Children?.Clear();
        RaiseJointUpdatedEvent(this);
    }

    public J18nJoint ShadowClone( )
    {
        return new J18nJoint()
        {
            Index = this.Index ,
            Key = this._key ,
            RawText = this.RawText ,
            Type = this.Type ,
            Comment = this.Comment ,
            Description = this.Description ,
            Path = this.Path ,
            CreationTime = this.CreationTime ,
            ModificationTime = this.ModificationTime ,
            Children = this.Children.Select(c => c.ShadowClone()).ToHashSet() ,
        };
    }

    public J18nJoint DeepClone(CancellationToken? ctsToken)
    {
        var clone = ShadowClone();

        ParallelOptions parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount - 2 ,
            CancellationToken = ctsToken ?? CancellationToken.None
        };

        if(clone.Children is not null)
        {
            var result = Parallel.ForEach(clone.Children , parallelOptions , child =>
            {
                child.Parent = clone;
                child.DeepClone(ctsToken);
            });

            while(!result.IsCompleted)
            {
                Thread.Sleep(100);
            }
        }

        return clone;
    }

    public async Task<J18nJoint> DeepCloneAsync(CancellationToken? ctsToken)
    {
        var clone = ShadowClone();

        ParallelOptions parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount - 2 ,
            CancellationToken = ctsToken ?? CancellationToken.None
        };

        if(clone.Children is not null)
        {
            var result = Parallel.ForEach(clone.Children , parallelOptions , child =>
            {
                child.Parent = clone;
                child.DeepClone(ctsToken);
            });

            while(!result.IsCompleted)
            {
                await Task.Delay(100);
            }
        }

        return clone;
    }

    object ICloneable.Clone( )
    {
        return DeepClone(CancellationToken.None);
    }

    public override int GetHashCode( )
    {
        int hash = 17;
        hash = hash * 23 + Index.GetHashCode();
        hash = hash * 23 + (Path?.GetHashCode() ?? 0);
        hash = hash * 23 + (Key?.GetHashCode() ?? 0);

        return hash;
    }

    public override bool Equals(object? obj)
    {
        if(obj == null || this.GetType() != obj.GetType())
            return false;

        J18nJoint other = (J18nJoint)obj;

        return this.Index == other.Index &&
            string.Equals(Path , other.Path , StringComparison.Ordinal) &&
            string.Equals(Key , other.Key , StringComparison.Ordinal);
    }

}

public class RawTextExchangArg : EventArgs
{
    public string? OldRawText { get; set; }
    public string? NewRawText { get; set; }
    [NotNull] public J18nJoint? Joint { get; set; }

    public RawTextExchangArg( ) { }

    public RawTextExchangArg(string? oldRawText , string? newRawText , J18nJoint? joint)
    {
        OldRawText = oldRawText;
        NewRawText = newRawText;
        Joint = joint;
    }
}
