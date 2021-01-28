using System.Diagnostics;

// ReSharper disable CheckNamespace

namespace System
{
    [DebuggerStepThrough]
    public static class ThinkingStringExtensions
    {
        /// <summary>
        /// Indicates whether this string is null or an System.String.Empty string.
        /// </summary>
        public static bool IsNullOrEmpty(this string? str) => string.IsNullOrEmpty(str);

        /// <summary>
        /// indicates whether this string is null, empty, or consists only of white-space characters.
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string? str) => string.IsNullOrWhiteSpace(str);
    }
}