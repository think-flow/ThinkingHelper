using System;
using System.Collections.Generic;
using Xunit;

namespace ThinkingHelper.Test.Extensions;

public class ThinkingStringExtensionsTest
{
    [Fact]
    public void Format_Str_ShouldNotThrowException()
    {
        var paras = new Dictionary<string, object?>
        {
            {"A", "qwe"}, {"B", "qwe kjlj"}, {"C", null}, {"D", 20}
        };
        string str = "${A}abd${D:N2}f$$${B}{${A}$$}$${qwe${C}";
        string actual = str.Format(paras);
        Assert.Equal("qweabd20.00f$qwe kjlj{qwe$}${qwe", actual);
    }

    [Fact]
    public void Format_Str_ShouldThrowException()
    {
        var paras = new Dictionary<string, string>
        {
            {"A", "qwe"}, {"B", "qwe kjlj"}
        };
        string str = "12$12";
        Assert.Throws<FormatException>(() => str.Format(paras));
        str = "123123${123";
        Assert.Throws<FormatException>(() => str.Format(paras));
        str = "qwe${}dddd";
        Assert.Throws<FormatException>(() => str.Format(paras));
        str = "qwe${UserName}dddd";
        Assert.Throws<FormatException>(() => str.Format(paras));
        str = "qwe${A:}dddd";
        Assert.Throws<FormatException>(() => str.Format(paras));
    }

    [Fact]
    public void Format_Str_ShouldReferenceEqual()
    {
        var paras = new Dictionary<string, object?>
        {
            {"A", "qwe"}, {"B", null}
        };
        string str = "12adfkljajsdfl12";
        string actual = str.Format(paras);
        Assert.True(ReferenceEquals(str, actual));
    }

    [Fact]
    public void Format_object_Str_ShouldNotThrowException()
    {
        var paras = new
        {
            A = "qwe", B = new { Name= "qwe kjlj" }, Time = DateTime.Parse("2022-2-9 13:58:32")
        };
        string str = "${A}abdf$$${B.Name.Length:N2}{${A}$$}$${qwe ${Time:yyyy-MM-dd HH:mm:ss}";
        string actual = str.Format(paras);
        Assert.Equal("qweabdf$8.00{qwe$}${qwe 2022-02-09 13:58:32", actual);
    }
}