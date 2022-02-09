using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace ThinkingHelper.Reflection.Extensions;

public static class ThinkingReflectionExtensions
{
    /// <summary>
    /// 将一个对象，转为字典表示。只获取对象公开的并具有读取器的实例属性
    /// </summary>
    public static Dictionary<string, object?> ToDictionary(this object obj)
    {
        return ToDictionary(obj, info => info.GetValue(obj));
    }

    /// <summary>
    /// 将一个对象，转为字典表示。只获取对象公开的并具有读取器的实例属性
    /// </summary>
    public static Dictionary<string, TValue> ToDictionary<TValue>(this object obj, Func<PropertyInfo, TValue> elementSelector)
    {
        Check.NotNull(obj);
        Check.NotNull(elementSelector);
        return obj.GetType()
            .GetProperties(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(info => info.Name, elementSelector);
    }
}