using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ThinkingHelper;

// ReSharper disable CheckNamespace
namespace System;

[DebuggerStepThrough]
public static class ThinkingStringExtensions
{
    /// <summary>
    /// Indicates whether this string is null or an System.String.Empty string.
    /// </summary>
    public static bool IsNullOrEmpty(this string? str) => string.IsNullOrEmpty(str);

    /// <summary>
    /// Indicates whether this string is null, empty, or consists only of white-space characters.
    /// </summary>
    public static bool IsNullOrWhiteSpace(this string? str) => string.IsNullOrWhiteSpace(str);

    /// <summary>
    /// Replaces one or more ${[ParameterName]} format items in a string with values in the argument dictionary.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">argument dictionary</param>
    /// <exception cref="ArgumentNullException">format or args are null</exception>
    /// <exception cref="FormatException">The format is invalid. -or- The value of the args is not found.</exception>
    /// <remarks>$$ will be escaped as $</remarks>
    /// <returns>A copy of format in which any format items are replaced.</returns>
    public static string Format(this string format, IDictionary<string, string?> args)
    {
        Check.NotNull(format);
        Check.NotNull(args);
        if (format.Length < 1) return format;

        var builder = new StringBuilder(format.Length + format.Length / 3);
        int startIndex = 0; //变量名开始下标
        int endIndex = 0; //变量名结束下标
        int state = 0; //0 初始状态 1 普通字符  2 $ 变量标签  3 { 变量开始  4 } 变量结束
        bool isSet = false;

        for (int i = 0; i < format.Length; i++)
        {
            char c = format[i];
            switch (state)
            {
                case 0:
                case 1:
                    if (c == '$')
                    {
                        state = 2;
                        continue;
                    }

                    builder.Append(c);
                    continue;
                case 2:
                    if (c == '$')
                    {
                        //两个$$ 转义为$
                        state = 1;
                        builder.Append('$');
                        continue;
                    }

                    if (c == '{')
                    {
                        state = 3;
                        startIndex = i + 1;
                        continue;
                    }

                    throw new FormatException($"Invalid placeholder! character $ must be followed by $, or{{ index:{i}");
                case 3:
                    if (c == '}')
                    {
                        isSet = true;
                        state = 1;
                        int index = endIndex - startIndex;
                        if (index < 0)
                        {
                            throw new FormatException($"Empty name of parameter! index:{startIndex}");
                        }

                        string paraName = format.Substring(startIndex, index + 1);
                        if (!args.TryGetValue(paraName, out string? value))
                        {
                            throw new FormatException($"The Value of parameter \"{paraName}\" not found! index:{startIndex}");
                        }

                        builder.Append(value);
                        continue;
                    }

                    endIndex = i;
                    continue;
                default:
                    throw new Exception("Unknown State! Please contact the owner!");
            }
        }

        return state switch
        {
            2 => throw new FormatException($"Invalid placeholder! character $ must be followed by $, or{{ index:{format.Length}"),
            3 => throw new FormatException($"Placeholder not closed! index:{startIndex}"),
            _ => isSet ? builder.ToString() : format
        };
    }

    /// <summary>
    /// Replaces one or more ${[ParameterName]} format items in a string with values in the argument object.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">argument object</param>
    /// <exception cref="ArgumentNullException">format or args are null</exception>
    /// <exception cref="FormatException">The format is invalid. -or- The value of the args is not found.</exception>
    /// <remarks>$$ will be escaped as $</remarks>
    /// <returns>A copy of format in which any format items are replaced.</returns>
    public static string Format(this string format, object args)
    {
        Check.NotNull(args);
        var argsDictionary = args.GetType().GetProperties().ToDictionary(info => info.Name, info => info.GetValue(args)?.ToString());
        return Format(format, argsDictionary);
    }
}