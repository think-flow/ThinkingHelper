using System;
using System.Collections.Generic;
using Xunit;

namespace ThinkingHelper.Test.Extensions;

public class ThinkingStringExtensionsTest
{
    [Fact]
    public void Format_Str_ShouldNotThrowException()
    {
        var paras = new Dictionary<string, string>
        {
            {"A", "qwe"}, {"B", "qwe kjlj"}
        };
        var str = "${A}abdf${B}{${A}$$}$${qwe";
        string actual = str.Format(paras);
        Assert.Equal("qweabdfqwe kjlj{qwe$}${qwe", actual);
    }

    [Fact]
    public void Format_Str_ShouldThrowException()
    {
        var paras = new Dictionary<string, string>
        {
            {"A", "qwe"}, {"B", "qwe kjlj"}
        };
        var str = "12$12";
        Assert.Throws<FormatException>(() => str.Format(paras));
        str = "123123${123";
        Assert.Throws<FormatException>(() => str.Format(paras));
    }

    [Fact]
    public void Format_Str_ShouldReferenceEqual()
    {
        var paras = new Dictionary<string, string>
        {
            {"A", "qwe"}, {"B", "qwe kjlj"}
        };
        var str = "12adfkljajsdfl12";
        string actual = str.Format(paras);
        Assert.True(ReferenceEquals(str, actual));
    }
}