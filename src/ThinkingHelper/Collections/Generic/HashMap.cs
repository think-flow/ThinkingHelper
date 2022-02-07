#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace ThinkingHelper.Collections.Generic;

/// <summary>
/// 一种当key不存在或key为null时，获取value也不会抛异常的只读集合
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
[Serializable]
[DebuggerTypeProxy(typeof(DictionaryDebugView<,>))]
[DebuggerDisplay("Count = {Count}")]
public class HashMap<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, IDictionary<TKey, TValue>, IDictionary
{
    private TValue _defaultValue;

    public HashMap(IDictionary<TKey, TValue> dictionary)
        : this(dictionary, default)
    {
    }

    public HashMap(IDictionary<TKey, TValue> dictionary, [AllowNull] TValue defaultValue)
    {
        Check.NotNull(dictionary);
        Dictionary = new ReadOnlyDictionary<TKey, TValue>(dictionary);
        _defaultValue = defaultValue;
    }

    protected IReadOnlyDictionary<TKey, TValue> Dictionary { get; }

    [NotNull] public ReadOnlyDictionary<TKey, TValue>.KeyCollection Keys => ((ReadOnlyDictionary<TKey, TValue>) Dictionary).Keys;

    [NotNull] public ReadOnlyDictionary<TKey, TValue>.ValueCollection Values => ((ReadOnlyDictionary<TKey, TValue>) Dictionary).Values;

    public bool TryGetValue([AllowNull] TKey key, [MaybeNull] out TValue value)
    {
        if (ContainsKey(key)) return Dictionary.TryGetValue(key!, out value);
        value = _defaultValue;
        return true;
    }

    [return: NotNull]
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => Dictionary.GetEnumerator();

    public int Count => Dictionary.Count;

    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
    public bool ContainsKey([AllowNull] TKey key) => key != null && Dictionary.ContainsKey(key);

    [MaybeNull]
    public TValue this[[AllowNull] TKey key]
    {
        get
        {
            TryGetValue(key, out var value);
            return value;
        }
    }

    /// <summary>
    /// 设置key不存在时，返回的默认值
    /// </summary>
    /// <param name="defaultValue"></param>
    public void SetDefaultValue([AllowNull] TValue defaultValue) => _defaultValue = defaultValue;

    #region Implement Interface

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) Dictionary).GetEnumerator();

    bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value) => TryGetValue(key, out value);

    void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => ((IDictionary<TKey, TValue>) Dictionary).Add(key, value);

    bool IDictionary<TKey, TValue>.Remove(TKey key) => ((IDictionary<TKey, TValue>) Dictionary).Remove(key);

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>) Dictionary).Add(item);

    void ICollection<KeyValuePair<TKey, TValue>>.Clear() => ((ICollection<KeyValuePair<TKey, TValue>>) Dictionary).Clear();

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>) Dictionary).Contains(item);

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        => ((ICollection<KeyValuePair<TKey, TValue>>) Dictionary).CopyTo(array, arrayIndex);

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        => ((ICollection<KeyValuePair<TKey, TValue>>) Dictionary).Remove(item);

    void IDictionary.Add(object key, object value) => ((IDictionary) Dictionary).Add(key, value);

    void IDictionary.Clear() => ((IDictionary) Dictionary).Clear();

    bool IDictionary.Contains(object key) => ContainsKey((TKey) key);

    IDictionaryEnumerator IDictionary.GetEnumerator() => ((IDictionary) Dictionary).GetEnumerator();

    void IDictionary.Remove(object key) => ((IDictionary) Dictionary).Remove(key);

    void ICollection.CopyTo(Array array, int index) => ((ICollection) Dictionary).CopyTo(array, index);

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Dictionary.Keys;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Dictionary.Values;

    ICollection<TKey> IDictionary<TKey, TValue>.Keys => ((IDictionary<TKey, TValue>) Dictionary).Keys;

    ICollection<TValue> IDictionary<TKey, TValue>.Values => ((IDictionary<TKey, TValue>) Dictionary).Values;

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => true;

    bool IDictionary.IsFixedSize => ((IDictionary) Dictionary).IsFixedSize;

    bool IDictionary.IsReadOnly => true;

    ICollection IDictionary.Keys => ((IDictionary) Dictionary).Keys;

    ICollection IDictionary.Values => ((IDictionary) Dictionary).Values;

    bool ICollection.IsSynchronized => ((ICollection) Dictionary).IsSynchronized;

    object ICollection.SyncRoot => ((ICollection) Dictionary).SyncRoot;

    object IDictionary.this[object key]
    {
        get => this[(TKey) key];
        set => ((IDictionary) Dictionary)[key] = value;
    }

    TValue IDictionary<TKey, TValue>.this[TKey key]
    {
        get => this[key];
        set => ((IDictionary<TKey, TValue>) Dictionary)[key] = value;
    }

    #endregion
}