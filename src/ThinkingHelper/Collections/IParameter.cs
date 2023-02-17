using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace ThinkingHelper.Collections;

internal interface IParameter
{
    bool TryGetValue(string name, out object? value);
}

internal sealed class DictionaryParameter : IParameter
{
    private readonly IDictionary _dic;

    public DictionaryParameter(IDictionary dic)
    {
        Check.NotNull(dic);
        _dic = dic;
    }

    public bool TryGetValue(string name, out object? value)
    {
        Check.NotNull(name);
        value = null;
        bool result = false;

        if (_dic.Contains(name))
        {
            value = _dic[name];
            result = true;
        }

        return result;
    }
}

internal sealed class ObjectParameter : IParameter
{
    private readonly Dictionary<string, object?> _cache;
    private readonly object _obj;

    public ObjectParameter(object obj)
    {
        Check.NotNull(obj);
        _obj = obj;
        _cache = new Dictionary<string, object?>();
    }

    public bool TryGetValue(string name, out object? value)
    {
        Check.NotNull(name);
        value = null;

        //从缓存中读取值
        if (_cache.TryGetValue(name, out object? cacheValue))
        {
            value = cacheValue;
            return true;
        }

        string[] parts = name.Split('.');
        if (parts.Length == 0)
        {
            return false;
        }

        object? obj = _obj;
        var stack = new Stack<string>(1);

        //例如 A.B.Name 缓存中如果没有
        //则尝试从缓存中读取 A.B的值，如果还没有
        //则尝试从缓存中读取 A的值
        for (int i = parts.Length - 1; i >= 1; i--)
        {
            string propertyName = string.Join('.', parts[..i]);
            if (_cache.TryGetValue(propertyName, out cacheValue))
            {
                obj = cacheValue;
                stack.Push(propertyName);
                parts = parts[i..];
                break;
            }
        }

        for (int i = 0; i < parts.Length; i++)
        {
            if (obj == null)
            {
                throw new NullReferenceException($"\"{string.Join('.', parts[..i])}\" in the parameter \"{name}\" is null");
            }

            var propertyInfo = obj.GetType().GetProperty(parts[i],
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
            if (propertyInfo?.CanRead != true)
            {
                return false;
            }

            obj = propertyInfo.GetValue(obj);
            //将每个读取过的值都进行缓存
            string propertyName = stack.TryPop(out string? prev) ? string.Concat(prev, ".", parts[i]) : parts[i];
            _cache.TryAdd(propertyName, obj);
            stack.Push(propertyName);
        }

        value = obj;
        return true;
    }
}