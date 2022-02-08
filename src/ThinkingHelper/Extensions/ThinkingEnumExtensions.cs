using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ThinkingHelper;

// ReSharper disable CheckNamespace

namespace System;

public static class ThinkingEnumExtensions
{
    /// <summary>
    /// 将<see cref="string" />转为<see cref="Enum" />类型
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="value"></param>
    /// <exception cref="ArgumentException">TEnum is not an System.Enum type.</exception>
    /// <exception cref="ArgumentException">value does not contain enumeration information.</exception>
    /// <returns></returns>
    public static TEnum ToEnum<TEnum>(this string value)
        where TEnum : struct
        => value.ToEnum<TEnum>(false);

    /// <summary>
    /// 将<see cref="string" />转为<see cref="Enum" />类型
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="value"></param>
    /// <param name="ignoreCase">是否忽略大小写</param>
    /// <exception cref="ArgumentException">TEnum is not an System.Enum type.</exception>
    /// <exception cref="ArgumentException">value does not contain enumeration information.</exception>
    /// <returns></returns>
    public static TEnum ToEnum<TEnum>(this string value, bool ignoreCase)
        where TEnum : struct
    {
        Check.NotNull(value);
        return Enum.Parse<TEnum>(value, ignoreCase);
    }

    /// <summary>
    /// 将<see cref="int" />转为<see cref="Enum" />类型
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="value"></param>
    /// <exception cref="ArgumentException">TEnum is not an System.Enum type.</exception>
    /// <returns></returns>
    public static TEnum ToEnum<TEnum>(this int value)
        where TEnum : struct =>
        value.ToEnum<TEnum>(false);

    /// <summary>
    /// 将<see cref="int" />转为<see cref="Enum" />类型
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="value"></param>
    /// <param name="checkDefined">检查value是否是枚举的有效值</param>
    /// <exception cref="ArgumentException">TEnum is not an System.Enum type.</exception>
    /// <exception cref="ArgumentException">value does not contain enumeration information.</exception>
    /// <returns></returns>
    public static TEnum ToEnum<TEnum>(this int value, bool checkDefined)
        where TEnum : struct
    {
        Type enumType = typeof(TEnum);

        if (!IsEnum(enumType))
        {
            throw new ArgumentException("TEnum is not an System.Enum type.");
        }

        if (checkDefined && !Enum.IsDefined(enumType, value))
        {
            throw new ArgumentException("value does not contain enumeration information.");
        }

        return Unsafe.As<int, TEnum>(ref value);
        //return (TEnum)Enum.ToObject(enumType, value);
    }

    /// <summary>
    /// 获取枚举上的<see cref="DescriptionAttribute" />的Description值
    /// </summary>
    /// <param name="value"></param>
    /// <returns>如果DescriptionAttribute不存在或Description为null则返回null</returns>
    public static string? GetDescription(this Enum value)
    {
        Check.NotNull(value);
        MemberInfo? memberInfo = value.GetType().GetMember(value.ToString(),
            MemberTypes.Field, BindingFlags.Public | BindingFlags.Static).FirstOrDefault();
        return memberInfo?.GetCustomAttribute<DescriptionAttribute>()?.Description;
    }

    #region Private

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsEnum<TEnum>(TEnum value)
        where TEnum : struct =>
        IsEnum(typeof(TEnum));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsEnum(Type type) => Check.NotNull(type).IsEnum;

    #endregion
}