using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ThinkingHelper;

// ReSharper disable CheckNamespace
namespace System.Linq;

public static partial class ThinkingLinqExtensions
{
    public static async ValueTask<List<T>> ToListAsync<T>(this Task<IEnumerable<T>> source)
    {
        var enumerable = await Check.NotNull(source).ConfigureAwait(false);
        return enumerable as List<T> ?? enumerable.ToList();
    }

    public static async ValueTask<T[]> ToArrayAsync<T>(this Task<IEnumerable<T>> source)
    {
        var enumerable = await Check.NotNull(source).ConfigureAwait(false);
        return enumerable as T[] ?? enumerable.ToArray();
    }

    public static async ValueTask<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
    {
        var enumerable = Check.NotNull(source);
        var list = new List<T>();
        await foreach (var item in enumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            list.Add(item);
        }

        return list;
    }

    public static async ValueTask<T[]> ToArrayAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
    {
        var enumerable = Check.NotNull(source);
        var list = await enumerable.ToListAsync(cancellationToken).ConfigureAwait(false);
        return list.ToArray();
    }

    /*
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
    */
}