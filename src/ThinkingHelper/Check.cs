using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ThinkingHelper;

[DebuggerStepThrough]
public static class Check
{
    [return: NotNull]
    public static T NotNull<T>(T? value, [CallerArgumentExpression("value")] string? paramName = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }

        return value;
    }

    public static string NotNullOrWhiteSpace(string? value, [CallerArgumentExpression("value")] string? paramName = null)
    {
        if (value.IsNullOrWhiteSpace())
        {
            throw new ArgumentException($"{paramName} can not be null, empty or white space!", paramName);
        }

        return value!;
    }

    public static string NotNullOrEmpty(string? value, [CallerArgumentExpression("value")] string? paramName = null)
    {
        if (value.IsNullOrEmpty())
        {
            throw new ArgumentException($"{paramName} can not be null or empty!", paramName);
        }

        return value!;
    }

    public static ICollection<T> NotNullOrEmpty<T>(ICollection<T>? value, [CallerArgumentExpression("value")] string? paramName = null)
    {
        if (value.IsNullOrEmpty())
        {
            throw new ArgumentException($"{paramName} can not be null or empty!", paramName);
        }

        return value!;
    }
}