using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using JetBrains.Annotations;
using ThinkingHelper;

// ReSharper disable CheckNamespace

namespace System
{
    [DebuggerStepThrough]
    public static class ThinkingStringExtensions
    {
        /// <summary>
        /// Indicates whether this string is null or an System.String.Empty string.
        /// </summary>
        [ContractAnnotation("str:null => true")]
        public static bool IsNullOrEmpty(this string? str) => string.IsNullOrEmpty(str);

        /// <summary>
        /// indicates whether this string is null, empty, or consists only of white-space characters.
        /// </summary>
        [ContractAnnotation("str:null => true")]
        public static bool IsNullOrWhiteSpace(this string? str) => string.IsNullOrWhiteSpace(str);

        /// <summary>
        /// 将字符串中的${[ParaName]}用字典中对应Key的Value进行替换
        /// $$将转义为$
        /// <param name="args">参数字典</param>
        /// <param name="ignoreCase">是否忽略参数大小写</param>
        /// </summary>
        public static string Format(this string format, IDictionary<string, string> args, bool ignoreCase = false)
        {
            Check.NotNull(format, nameof(format));
            Check.NotNull(args, nameof(args));
            if (format.Length == 0) return format;
            if (ignoreCase) args = new Dictionary<string, string>(args, StringComparer.OrdinalIgnoreCase);

            //$$ 转义为$
            var builder = new StringBuilder(format.Length + format.Length / 3);
            int startIndex = 0; //变量名开始下标
            int endIndex = 0; //变量名结束下标
            int state = 1; // 1 普通字符  2 $ 可能是变量)  3 { 变量开始  4 } 变量结束
            bool isSet = false;

            for (var i = 0; i < format.Length; i++)
            {
                char c = format[i];
                switch (state)
                {
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
                        throw new FormatException($"Ch:{i} 无效的占位符定义！$后必须为$，或者{{");
                    case 3:
                        if (c == '}')
                        {
                            isSet = true;
                            state = 1;
                            int index = endIndex - startIndex;
                            if (index < 0)
                            {
                                throw new FormatException($"Ch:{startIndex} 空参数定义！");
                            }
                            string paraName = format.Substring(startIndex, index + 1);
                            if (!args.TryGetValue(paraName, out string? value))
                            {
                                throw new FormatException($"未找到参数{paraName}的值");
                            }
                            builder.Append(value);
                            continue;
                        }
                        endIndex = i;
                        continue;
                    default:
                        throw new Exception("未知状态");
                }
            }

            return state switch
            {
                2 => throw new FormatException($"Ch:{format.Length} 无效的占位符定义！$后必须为$，或者{{"),
                3 => throw new FormatException($"Ch:{startIndex} 占位符定义未闭合"),
                _ => isSet ? builder.ToString() : format
            };
        }
    }
}