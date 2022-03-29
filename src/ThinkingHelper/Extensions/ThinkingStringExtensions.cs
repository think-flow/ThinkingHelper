using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using ThinkingHelper;
using ThinkingHelper.Collections;
using ThinkingHelper.Reflection.Extensions;

// ReSharper disable CheckNamespace
namespace System;

public static class ThinkingStringExtensions
{
    /// <summary>
    /// Indicates whether this string is null or an System.String.Empty string.
    /// </summary>
    [DebuggerStepThrough]
    public static bool IsNullOrEmpty(this string? str) => string.IsNullOrEmpty(str);

    /// <summary>
    /// Indicates whether this string is null, empty, or consists only of white-space characters.
    /// </summary>
    [DebuggerStepThrough]
    public static bool IsNullOrWhiteSpace(this string? str) => string.IsNullOrWhiteSpace(str);

    /// <summary>
    /// Replaces one or more ${[ParameterName]} format items in a string with values in the argument object or dictionary.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="args">argument object or dictionary</param>
    /// <exception cref="ArgumentNullException">format or args are null</exception>
    /// <exception cref="FormatException">The format is invalid. -or- The value of the args is not found.</exception>
    /// <remarks>$$ will be escaped as $</remarks>
    /// <returns>A copy of format in which any format items are replaced.</returns>
    public static string Format(this string format, object args)
    {
        Check.NotNull(format);
        Check.NotNull(args);

        if (args is IDictionary dicArgs)
        {
            return FormatCore(format, new DictionaryParameter(dicArgs));
        }
        return FormatCore(format, new ObjectParameter(args));

        static string FormatCore(string format, IParameter args)
        {
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

                            //[parameterName:format-string]
                            string paraPattern = format.Substring(startIndex, index + 1);
                            //解析出参数名称和格式化字符串
                            string[] patternParts = paraPattern.Split(':', 2);
                            string paraName = patternParts[0];
                            if (!args.TryGetValue(paraName, out object? value))
                            {
                                throw new FormatException($"The parameter \"{paraName}\" is not found in the argument dictionary! index:{startIndex}");
                            }

                            string? paraFormat = null;
                            if (patternParts.Length == 2)
                            {
                                paraFormat = patternParts[1];
                            }

                            if (paraFormat == null || value == null)
                            {
                                //不存在格式化字符串时的处理
                                builder.Append(value);
                            }
                            else
                            {
                                //存在格式化字符串时的处理
                                if (value is IFormattable formatValue)
                                {
                                    builder.Append(formatValue.ToString(paraFormat, CultureInfo.CurrentCulture));
                                }
                                else
                                {
                                    throw new FormatException($"The type \"{value.GetType().FullName}\" of the value corresponding to parameter \"{paraName}\" does not implement IFormattable interface! index:{startIndex}");
                                }
                            }

                            continue;
                        }

                        endIndex = i;
                        continue;
                    default:
                        throw new Exception("Unknown State! Please contact the lib owner!");
                }
            }

            return state switch
            {
                2 => throw new FormatException($"Invalid placeholder! character $ must be followed by $, or{{ index:{format.Length}"),
                3 => throw new FormatException($"Placeholder not closed! index:{startIndex}"),
                _ => isSet ? builder.ToString() : format
            };
        }
    }
}