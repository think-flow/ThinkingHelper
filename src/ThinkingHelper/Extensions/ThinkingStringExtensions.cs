using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    /// indicates whether this string is null, empty, or consists only of white-space characters.
    /// </summary>
    public static bool IsNullOrWhiteSpace(this string? str) => string.IsNullOrWhiteSpace(str);

    /// <summary>
    /// 将字符串中的${[ParaName]}用字典中对应Key的Value进行替换
    /// $$将转义为$
    /// <param name="args">参数字典</param>
    /// <param name="ignoreCase">是否忽略参数大小写</param>
    /// </summary>
    public static string Format(this string format, IDictionary<string, string> args, bool ignoreCase = false)
    {
        Check.NotNull(format);
        Check.NotNull(args);
        if (format.Length < 1) return format;
        if (ignoreCase) args = new Dictionary<string, string>(args, StringComparer.OrdinalIgnoreCase);

        //$$ 转义为$
        var builder = new StringBuilder(format.Length + format.Length / 3);
        var startIndex = 0; //变量名开始下标
        var endIndex = 0; //变量名结束下标
        var state = 0; //0 初始状态 1 普通字符  2 $ 变量标签  3 { 变量开始  4 } 变量结束
        var isSet = false;

        for (var i = 0; i < format.Length; i++)
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

                    throw new FormatException(GetIvalidPlaceholderMessage(i));
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
                            throw new FormatException($"Value of parameter \"{paraName}\" not found! index:{startIndex}");
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
            2 => throw new FormatException(GetIvalidPlaceholderMessage(format.Length)),
            3 => throw new FormatException($"Placeholder not closed! index:{startIndex}"),
            _ => isSet ? builder.ToString() : format
        };

        static string GetIvalidPlaceholderMessage(int index)
            => $"Invalid placeholder! character $ must be followed by $, or{{ index:{index}";
    }
}