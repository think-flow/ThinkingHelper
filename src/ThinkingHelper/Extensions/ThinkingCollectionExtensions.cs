using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable All

namespace System.Collections.Generic
{
    [DebuggerStepThrough]
    public static class ThinkingCollectionExtensions
    {
        /// <summary>
        /// Checks whatever given collection object is null or has no item.
        /// </summary>
        public static bool IsNullOrEmpty<T>(this ICollection<T>? source) =>
            source == null || source.Count <= 0;

        public static async Task<List<T>> ToListAsync<T>(this Task<IEnumerable<T>> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var enumerable = await source;
            return enumerable is List<T> list ? list : enumerable.ToList();
        }

        public static async Task<T[]> ToArrayAsync<T>(this Task<IEnumerable<T>> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var enumerable = await source;
            return enumerable is T[] array ? array : enumerable.ToArray();
        }

        public static HashMap<TKey, TSource> ToHashMap<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            where TKey : notnull
            => new(source.ToDictionary(keySelector)!, default);

        public static HashMap<TKey, TElement> ToHashMap<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
            where TKey : notnull
            => new(source.ToDictionary(keySelector, elementSelector)!, default);
    }
}