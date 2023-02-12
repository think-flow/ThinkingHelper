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
[SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
public sealed class HashMap<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, IDictionary<TKey, TValue>, IDictionary
    where TKey : notnull
{
    private readonly ReadOnlyDictionary<TKey, TValue> _dic;

    private TValue _defaultValue;

    public HashMap(IDictionary<TKey, TValue> dictionary, TValue defaultValue)
    {
        Check.NotNull(dictionary);
        _dic = new ReadOnlyDictionary<TKey, TValue>(dictionary);
        _defaultValue = defaultValue;
    }

    public ICollection<TKey> Keys => _dic.Keys;

    public ICollection<TValue> Values => _dic.Values;

    public int Count => _dic.Count;

    public bool ContainsKey(TKey? key) => key != null && _dic.ContainsKey(key);

    public bool TryGetValue(TKey? key, out TValue value)
    {
        value = ContainsKey(key) ? _dic[key!] : _defaultValue;
        return true;
    }

    public TValue this[TKey? key]
    {
        get
        {
            TryGetValue(key, out var value);
            return value;
        }
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() =>
        ((IEnumerable<KeyValuePair<TKey, TValue>>) _dic).GetEnumerator();

    /// <summary>
    /// 设置key不存在时，返回的默认值
    /// </summary>
    /// <param name="value"></param>
    public void SetDefaultValue(TValue value) => _defaultValue = value;

    #region Explicitly Implement Interface

    /*
     * 显示实现的接口中，也对Contains 索引器 进行了null处理
     */
    bool IDictionary.Contains(object key) => key != null && ((IDictionary) _dic).Contains(key);

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) =>
        item.Key != null && ((ICollection<KeyValuePair<TKey, TValue>>) _dic).Contains(item);

    TValue IDictionary<TKey, TValue>.this[TKey key]
    {
        get => this[key];
        set => ((IDictionary<TKey, TValue>) _dic)[key] = value;
    }

    object? IDictionary.this[object key]
    {
        get => key != null ? this[(TKey) key] : this[default];
        set => ((IDictionary) _dic)[key] = value;
    }

    ICollection IDictionary.Values => ((IDictionary) _dic).Values;

    ICollection IDictionary.Keys => ((IDictionary) _dic).Keys;

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys =>
        ((IReadOnlyDictionary<TKey, TValue>) _dic).Keys;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values =>
        ((IReadOnlyDictionary<TKey, TValue>) _dic).Values;

    bool IDictionary<TKey, TValue>.Remove(TKey key) => ((IDictionary<TKey, TValue>) _dic).Remove(key);

    void IDictionary<TKey, TValue>.Add(TKey key, TValue value) =>
        ((IDictionary<TKey, TValue>) _dic).Add(key, value);

    void IDictionary.Add(object key, object? value) =>
        ((IDictionary) _dic).Add(key, value);

    void IDictionary.Clear() => ((IDictionary) _dic).Clear();

    void ICollection<KeyValuePair<TKey, TValue>>.Clear() =>
        ((ICollection<KeyValuePair<TKey, TValue>>) _dic).Clear();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) _dic).GetEnumerator();

    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) =>
        ((ICollection<KeyValuePair<TKey, TValue>>) _dic).Add(item);

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
        ((ICollection<KeyValuePair<TKey, TValue>>) _dic).CopyTo(array, arrayIndex);

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) =>
        ((ICollection<KeyValuePair<TKey, TValue>>) _dic).Remove(item);

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly =>
        ((ICollection<KeyValuePair<TKey, TValue>>) _dic).IsReadOnly;

    void ICollection.CopyTo(Array array, int index) => ((ICollection) _dic).CopyTo(array, index);

    bool ICollection.IsSynchronized => ((ICollection) _dic).IsSynchronized;

    object ICollection.SyncRoot => ((ICollection) _dic).SyncRoot;

    IDictionaryEnumerator IDictionary.GetEnumerator() => ((IDictionary) _dic).GetEnumerator();

    void IDictionary.Remove(object key) => ((IDictionary) _dic).Remove(key);

    bool IDictionary.IsFixedSize => ((IDictionary) _dic).IsFixedSize;

    bool IDictionary.IsReadOnly => ((IDictionary) _dic).IsReadOnly;

    #endregion
}