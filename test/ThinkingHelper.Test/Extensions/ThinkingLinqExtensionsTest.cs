using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ThinkingHelper.Test.Extensions;

public class ThinkingLinqExtensionsTest
{
    [Fact]
    public void ForEach_ShouldNotThrowException()
    {
        string str = "AbC";
        List<char> charList = new List<char>();
        str.ForEach(i => charList.Add(i));
        Assert.Equal(3, charList.Count);
        Assert.Equal('b', charList[1]);
    }

    [Fact]
    public void Effect_ShouldNotThrowException()
    {
        string str = "AbC";
        List<char> charList = new List<char>();
        var enumerable = str.Effect(i => charList.Add(i));
        Assert.Empty(charList);
        List<char> charList2 = enumerable.ToList();
        Assert.Equal(3, charList.Count);
        Assert.Equal('b', charList[1]);
        Assert.Equal(3, charList2.Count);
        Assert.Equal('C', charList[2]);
    }
}