using System.Collections.Generic;
using ThinkingHelper;

// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable CheckNamespace
namespace System.Linq;

public static partial class ThinkingLinqExtensions
{
    /// <summary>
    /// 等同于foreach关键字
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <param name="action"></param>
    public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> action)
    {
        Check.NotNull(source);
        foreach (TSource item in source)
        {
            action(item);
        }
    }

    /// <summary>
    /// 等同于foreach，但不会终结linq管道
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="source"></param>
    /// <param name="effect"></param>
    /// <returns></returns>
    public static IEnumerable<TSource> Effect<TSource>(this IEnumerable<TSource> source, Action<TSource> effect)
    {
        Check.NotNull(source);
        foreach (TSource item in source)
        {
            effect(item);
            yield return item;
        }
    }
}