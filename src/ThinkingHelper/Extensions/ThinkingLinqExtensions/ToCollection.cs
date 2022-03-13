using System.Collections.Generic;
using System.Threading.Tasks;
using ThinkingHelper;
using ThinkingHelper.Collections.Generic;

// ReSharper disable CheckNamespace
namespace System.Linq;

public static partial class ThinkingLinqExtensions
{
    public static async Task<List<T>> ToListAsync<T>(this Task<IEnumerable<T>> source)
    {
        var enumerable = await Check.NotNull(source);
        return enumerable is List<T> list ? list : enumerable.ToList();
    }

    public static async Task<T[]> ToArrayAsync<T>(this Task<IEnumerable<T>> source)
    {
        var enumerable = await Check.NotNull(source);
        return enumerable is T[] array ? array : enumerable.ToArray();
    }

    public static HashMap<TKey, TSource> ToHashMap<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        where TKey : notnull
    {
        var dic = Check.NotNull(source).ToDictionary(keySelector);
        return new HashMap<TKey, TSource>(dic, default);
    }

    public static HashMap<TKey, TElement> ToHashMap<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        where TKey : notnull
    {
        var dic = Check.NotNull(source).ToDictionary(keySelector, elementSelector);
        return new HashMap<TKey, TElement>(dic, default);
    }
}