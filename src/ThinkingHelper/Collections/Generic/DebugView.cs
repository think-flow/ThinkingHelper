using System.Collections.Generic;
using System.Diagnostics;

namespace ThinkingHelper.Collections.Generic;

internal sealed class CollectionDebugView<T>
{
    private readonly ICollection<T> _collection;

    public CollectionDebugView(ICollection<T> collection)
    {
        _collection = Check.NotNull(collection);
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items
    {
        get
        {
            var items = new T[_collection.Count];
            _collection.CopyTo(items, 0);
            return items;
        }
    }
}

internal sealed class DictionaryDebugView<TKey, TValue>
{
    private readonly IDictionary<TKey, TValue> _dic;

    public DictionaryDebugView(IDictionary<TKey, TValue> dic)
    {
        _dic = Check.NotNull(dic);
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<TKey, TValue>[] Items
    {
        get
        {
            var items = new KeyValuePair<TKey, TValue>[_dic.Count];
            _dic.CopyTo(items, 0);
            return items;
        }
    }
}