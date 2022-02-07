using System;
using Xunit;

namespace ThinkingHelper.Test;

public class CheckTest
{
    [Fact]
    public void NotNull_Null_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Check.NotNull<string?>(null!));
    }

    [Fact]
    public void NotNull_EmptyArray_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Check.NotNullOrEmpty(Array.Empty<int>()));
    }
}