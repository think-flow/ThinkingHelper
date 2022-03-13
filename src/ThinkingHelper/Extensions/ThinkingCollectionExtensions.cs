using System.Diagnostics;

// ReSharper disable CheckNamespace
namespace System.Collections.Generic;

public static class ThinkingCollectionExtensions
{
    /// <summary>
    /// Checks whatever given collection object is null or has no item.
    /// </summary>
    [DebuggerStepThrough]
    public static bool IsNullOrEmpty<T>(this ICollection<T>? source) =>
        source == null || source.Count <= 0;
}