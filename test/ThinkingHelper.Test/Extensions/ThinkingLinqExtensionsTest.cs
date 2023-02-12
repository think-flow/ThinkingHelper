using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    [Fact]
    public async Task ToArrayAsync_ShouldNotThrowException()
    {
        var array = await GetDataAsync().ToArrayAsync();
        
        Assert.Equal(1000,array.Length);
        Assert.Equal(300,array[300]);
        Assert.Equal(769,array[769]);

        static async IAsyncEnumerable<int> GetDataAsync()
        {
            var enumerable = Enumerable.Range(0,1000);
            foreach (int item in enumerable)
            {
                yield  return item;
            }
            await Task.CompletedTask;
        }
    }
}