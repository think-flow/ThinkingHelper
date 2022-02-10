using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ThinkingHelper;

// ReSharper disable CheckNamespace

namespace ThinkingHelper.Reflection.Extensions
{
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
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(info => info.CanRead)
                .ToDictionary(info => info.Name, elementSelector);
        }
    }
}

namespace System.Reflection
{
    public static class ThinkingReflectionExtensions
    {
        /// <summary>
        /// Gets a value indicating whether the current type is a anonymous type.
        /// </summary>
        /// <returns>true if the current type is a anonymous type; otherwise, false.</returns>
        public static bool IsAnonymousType(this Type type)
        {
            Check.NotNull(type);
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute))
                   && type.Namespace == null
                   && type.IsClass
                   && !type.IsAbstract
                   && type.IsNotPublic
                   && type.IsGenericType
                   && type.IsSealed
                   && !type.IsSerializable
                   && type.BaseType == typeof(object)
                   && type.Name.Contains("AnonymousType");
        }
    }
}