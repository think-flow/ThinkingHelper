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

        /// <summary>
        /// Gets a value indicating whether the current type is a Simple type.
        /// </summary>
        /// <returns>true if the Simple type is a anonymous type; otherwise, false.</returns>
        public static bool IsSimpleType(this Type type)
        {
            /* 简单类型定义
             * 基元类型Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single
             * 非基元类型Enum, DateTime, DateTimeOffset, TimeSpan, Guid, string, decimal
             * 可空值类型
             */
            Check.NotNull(type);
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // nullable type, check if the nested type is simple.
                return IsSimpleType((type.GetGenericArguments()[0]).GetTypeInfo());
            }
            return type.IsPrimitive
                   || type.IsEnum
                   || type == typeof(DateTime)
                   || type == typeof(DateTimeOffset)
                   || type == typeof(TimeSpan)
                   || type == typeof(Guid)
                   || type == typeof(string)
                   || type == typeof(decimal);

            //或者以下代码
            //return System.ComponentModel.TypeDescriptor.GetConverter(type).CanConvertFrom(typeof(string));
        }
    }
}