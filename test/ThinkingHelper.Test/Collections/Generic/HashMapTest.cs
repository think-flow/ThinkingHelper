using System.Collections.Generic;
using ThinkingHelper.Collections.Generic;
using Xunit;

namespace ThinkingHelper.Test.Collections.Generic;

public class HashMapTest
{
    [Fact]
    public void Count_4Items_ShouldBe4()
    {
        var dic = new Dictionary<string, string>
        {
            {"A", "a"}, {"B", "b"}, {"C", "c"}, {"D", "d"}
        };
        var hashMap = new HashMap<string, string>(dic, null);
        Assert.Equal(4, hashMap.Count);
    }

    [Fact]
    public void ContainsKey_A_ShouldBeTrue()
    {
        var dic = new Dictionary<string, string>
        {
            {"A", "a"}, {"B", "b"}, {"C", "c"}, {"D", "d"}
        };
        var hashMap = new HashMap<string, string>(dic, null);
        bool value = hashMap.ContainsKey("A");
        Assert.True(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("F")]
    public void ContainsKey_NonExistentKey_ShouldBeFalse(string key)
    {
        var dic = new Dictionary<string, string>
        {
            {"A", "a"}, {"B", "b"}, {"C", "c"}, {"D", "d"}
        };
        var hashMap = new HashMap<string, string>(dic, null);
        bool value = hashMap.ContainsKey(key);
        Assert.False(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("F")]
    public void Indexer_NonExistentKey_ShouldBeNull(string key)
    {
        var dic = new Dictionary<string, string>
        {
            {"A", "a"}, {"B", "b"}, {"C", "c"}, {"D", "d"}
        };
        var hashMap = new HashMap<string, string>(dic, null);
        string? value = hashMap[key];
        Assert.Null(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("F")]
    public void Indexer_NonExistentKey_ShouldBeQ(string key)
    {
        var dic = new Dictionary<string, string>
        {
            {"A", "a"}, {"B", "b"}, {"C", "c"}, {"D", "d"}
        };
        var hashMap = new HashMap<string, string>(dic, "Q");
        string? value = hashMap[key];
        Assert.Equal("Q", value);
    }

    [Theory]
    [InlineData("C")]
    public void Indexer_ExistentKey_ShouldBec(string key)
    {
        var dic = new Dictionary<string, string>
        {
            {"A", "a"}, {"B", "b"}, {"C", "c"}, {"D", "d"}
        };
        var hashMap = new HashMap<string, string>(dic, null);
        string? value = hashMap[key];
        Assert.Equal("c", value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("F")]
    public void TryGetValue_NonExistentKey_ShouldBeNull(string key)
    {
        var dic = new Dictionary<string, string>
        {
            {"A", "a"}, {"B", "b"}, {"C", "c"}, {"D", "d"}
        };
        var hashMap = new HashMap<string, string>(dic, null);
        bool result = hashMap.TryGetValue(key, out string? value);
        Assert.True(result);
        Assert.Null(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("F")]
    public void TryGetValue_NonExistentKey_ShouldBeQ(string key)
    {
        var dic = new Dictionary<string, string>
        {
            {"A", "a"}, {"B", "b"}, {"C", "c"}, {"D", "d"}
        };
        var hashMap = new HashMap<string, string>(dic, "Q");
        bool result = hashMap.TryGetValue(key, out string? value);
        Assert.True(result);
        Assert.Equal("Q", value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("F")]
    public void SetDefaultValue_Q_ShouldBeQ(string key)
    {
        var dic = new Dictionary<string, string>
        {
            {"A", "a"}, {"B", "b"}, {"C", "c"}, {"D", "d"}
        };
        var hashMap = new HashMap<string, string>(dic, null);
        hashMap.SetDefaultValue("Q");
        Assert.Equal("Q", hashMap[key]);
    }
}