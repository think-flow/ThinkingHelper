using System;
using System.Reflection;
using ThinkingHelper.Reflection.Extensions;
using Xunit;

namespace ThinkingHelper.Test.Extensions;

public class TestClass
{
    private int _b;
    public string? A { get; set; }

    public static object? StatA { get; set; }

    public int B
    {
        set => _b = value;
    }

    private DateTime Time { get; set; } = DateTime.Now;
}

public class ThinkingReflectionExtensionsTest
{
    [Fact]
    public void ToDictionary_ShouldNotThrowException()
    {
        string aValue = "aaa";
        int bValue = 20;
        TestClass.StatA = new object();
        var testClass = new TestClass {A = aValue, B = bValue};
        var dic = testClass.ToDictionary(info => info.GetValue(testClass)?.ToString());
        Assert.Single(dic);
        Assert.Equal(dic["A"], aValue);
    }

    [Fact]
    public void IsAnonymousType_Type_ShouldBeFalse()
    {
        var testClass = new TestClass();
        bool actual = testClass.GetType().IsAnonymousType();
        Assert.False(actual);
    }

    [Fact]
    public void IsAnonymousType_Type_ShouldBeTrue()
    {
        var testClass = new {Id = 10};
        bool actual = testClass.GetType().IsAnonymousType();
        Assert.True(actual);
    }
}