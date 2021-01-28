using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ThinkingHelper.Validation
{
    /// <summary>
    /// 验证参数的值是否在给定的一组数值中
    /// </summary>
    public class ValueInAttribute : ValidationAttribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="parameterType">期望参数的类型</param>
        /// <param name="expectedValues">期望参数</param>
        public ValueInAttribute(Type parameterType, params object[] expectedValues)
        {
            Check.NotNull(parameterType, nameof(parameterType));
            Check.NotNullOrEmpty(expectedValues, nameof(expectedValues));
            if (expectedValues.Select(e => e.GetType()).Any(type => type != parameterType))
            {
                throw new ArgumentException($"{nameof(expectedValues)}中的元素类型应与{nameof(parameterType)}所指定的类型一致");
            }

            ParameterType = parameterType;
            ExpectedValues = expectedValues;
        }

        /// <summary>
        /// 期望参数的类型
        /// </summary>
        public Type ParameterType { get; }

        /// <summary>
        /// 期望参数
        /// </summary>
        public object[] ExpectedValues { get; set; }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            foreach (var expectedValue in ExpectedValues)
            {
                if (expectedValue.Equals(value))
                {
                    return ValidationResult.Success;
                }
            }

            return new ValidationResult($"参数{validationContext.MemberName}的值必须为[{string.Join(" 或 ", ExpectedValues)}]",
                new[] { validationContext.MemberName ?? string.Empty });
        }
    }
}