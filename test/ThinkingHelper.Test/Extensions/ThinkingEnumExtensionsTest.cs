using System;
using System.ComponentModel;
using Xunit;

namespace ThinkingHelper.Test.Extensions
{
    public enum TestEnum
    {
        Add = 1,
        [Description("删除")]
        Remove = 2,
        Insert = 45
    }

    public class ThinkingEnumExtensionsTest
    {
        [Fact]
        public void ToEnum_Str_ShouldNotThrowException()
        {
            var str = "Add";
            var te = str.ToEnum<TestEnum>();
            Assert.Equal(TestEnum.Add, te);
        }

        [Fact]
        public void ToEnum_Str_ShouldThrowException()
        {
            var str = "ABC";
            Assert.Throws<ArgumentException>(() => str.ToEnum<TestEnum>());
        }

        [Fact]
        public void ToEnum_Int_ShouldNotThrowException()
        {
            var value = 45;
            var te = value.ToEnum<TestEnum>();
            Assert.Equal(TestEnum.Insert, te);
        }

        [Fact]
        public void ToEnum_Int_ShouldThrowException()
        {
            var value = 777;
            Assert.Throws<ArgumentException>(() => value.ToEnum<TestEnum>(true));
        }

        [Fact]
        public void GetDescriptor_ShouldBeTrue()
        {
            string? description = TestEnum.Remove.GetDescription();
            Assert.Equal("删除", description);

            string? description2 = TestEnum.Add.GetDescription();
            Assert.Null(description2);
        }
    }
}