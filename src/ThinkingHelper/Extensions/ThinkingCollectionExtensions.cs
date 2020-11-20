using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;

// ReSharper disable CheckNamespace

namespace System.Collections.Generic
{
    [DebuggerStepThrough]
    public static class ThinkingCollectionExtensions
    {
        /// <summary>
        /// Checks whatever given collection object is null or has no item.
        /// </summary>
        public static bool IsNullOrEmpty<T>([CanBeNull] this ICollection<T> source) =>
            source == null || source.Count <= 0;

        public static async Task<List<T>> ToListAsync<T>([NotNull] this Task<IEnumerable<T>> source)
        {
            var enumerable = await source;
            return enumerable is List<T> list ? list : enumerable.ToList();
        }

        public static async Task<T[]> ToArrayAsync<T>([NotNull] this Task<IEnumerable<T>> source)
        {
            var enumerable = await source;
            return enumerable is T[] array ? array : enumerable.ToArray();
        }
    }
}