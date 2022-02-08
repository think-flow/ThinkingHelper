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
    /// <param name="obj"></param>
    /// <returns></returns>
    public static Dictionary<string, object?> ToDictionary(this object obj)
    {
        return Check.NotNull(obj).
            GetType().
            GetProperties(BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance).
            ToDictionary(info => info.Name, info => info.GetValue(obj));
    }
}