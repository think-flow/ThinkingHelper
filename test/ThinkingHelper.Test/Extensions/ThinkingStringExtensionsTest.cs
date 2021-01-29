using System;
using System.Collections.Generic;
using Xunit;

namespace ThinkingHelper.Test.Extensions
{
    public class ThinkingStringExtensionsTest
    {
        [Fact]
        public void Format_Str_ShouldNotThrowException()
        {
            Dictionary<string, string> paras = new Dictionary<string, string>
            {
                {"A", "qwe"}, {"B", "qwe kjlj"}
            };
            string str = "${A}abdf${B}{${A}$$}$${qwe";
            var actual = str.Format(paras);
            Assert.Equal("qweabdfqwe kjlj{qwe$}${qwe", actual);
        }

        [Fact]
        public void Format_Str_ShouldThrowException()
        {
            Dictionary<string, string> paras = new Dictionary<string, string>
            {
                {"A", "qwe"}, {"B", "qwe kjlj"}
            };
            string str = "12$12";
            Assert.Throws<FormatException>(() => str.Format(paras));
            str = "123123${123";
            Assert.Throws<FormatException>(() => str.Format(paras));
        }
    }
}