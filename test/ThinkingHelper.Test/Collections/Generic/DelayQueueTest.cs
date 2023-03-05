using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThinkingHelper.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace ThinkingHelper.Test.Collections.Generic;

public class DelayQueueTest
{
    private readonly ITestOutputHelper _output;

    public DelayQueueTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TryEnqueue_ReturnTrue()
    {
        // Arrange
        var delayQueue = new DelayQueue<TestDelayItem>();
        var now = DateTimeOffset.Now;

        // Act and Assert
        bool actual = delayQueue.TryEnqueue(new TestDelayItem(GetTimespan(now, 10000)));
        actual = actual && delayQueue.TryEnqueue(new TestDelayItem(GetTimespan(now, 11000)));
        actual = actual && delayQueue.TryEnqueue(new TestDelayItem(GetTimespan(now, 4000)));
        actual = actual && delayQueue.TryEnqueue(new TestDelayItem(GetTimespan(now, 5002)));
        Assert.True(actual);
    }

    [Fact]
    public void Snapshot_ShouldBeSuccessful()
    {
        // Arrange
        var delayQueue = new DelayQueue<TestDelayItem>();
        var now = DateTimeOffset.Now;
        delayQueue.TryEnqueue(new TestDelayItem(GetTimespan(now, 10000)));
        delayQueue.TryEnqueue(new TestDelayItem(GetTimespan(now, 11000)));

        // Act and Assert
        var collection = delayQueue.Snapshot();
        delayQueue.TryEnqueue(new TestDelayItem(GetTimespan(now, 4000)));
        delayQueue.TryEnqueue(new TestDelayItem(GetTimespan(now, 5002)));
        Assert.Equal(2, collection.Count);
    }

    [Fact]
    public void Count_4Items_ShouldBe4()
    {
        // Arrange
        var delayQueue = new DelayQueue<TestDelayItem>();
        var now = DateTimeOffset.Now;
        delayQueue.TryEnqueue(new TestDelayItem(GetTimespan(now, 10000)));
        delayQueue.TryEnqueue(new TestDelayItem(GetTimespan(now, 11000)));
        delayQueue.TryEnqueue(new TestDelayItem(GetTimespan(now, 4000)));
        delayQueue.TryEnqueue(new TestDelayItem(GetTimespan(now, 5002)));

        // Act and Assert
        Assert.Equal(4, delayQueue.Count);
    }

    [Fact]
    public void TryPeek_ReturnFalse()
    {
        // Arrange
        var delayQueue = new DelayQueue<TestDelayItem>();

        // Act and Assert
        bool actual = delayQueue.TryPeek(out _);
        Assert.False(actual);
    }

    [Fact]
    public void TryPeek_ShouldSame()
    {
        // Arrange
        var delayQueue = new DelayQueue<TestDelayItem>();
        var now = DateTimeOffset.Now;
        var item1 = new TestDelayItem(GetTimespan(now, 10000));
        var item2 = new TestDelayItem(GetTimespan(now, 11000));
        var item3 = new TestDelayItem(GetTimespan(now, 4000));
        var item4 = new TestDelayItem(GetTimespan(now, 5002));
        delayQueue.TryEnqueue(item1);
        delayQueue.TryEnqueue(item2);
        delayQueue.TryEnqueue(item3);
        delayQueue.TryEnqueue(item4);

        // Act and Assert
        bool successful = delayQueue.TryPeek(out var actualItem);
        Assert.True(successful);
        Assert.Same(item3, actualItem);
    }

    [Fact]
    public void TryDequeue_ItemsIsEmpty()
    {
        // Arrange
        var delayQueue = new DelayQueue<TestDelayItem>();
        var sw = new Stopwatch();

        // Act and Assert
        TestDelayItem? actualItem;
        bool result = delayQueue.TryDequeue(out actualItem);
        Assert.False(result);
        Assert.Null(actualItem);

        sw.Restart();
        result = delayQueue.TryDequeue(out actualItem, 500);
        sw.Stop();
        Assert.False(result);
        Assert.Null(actualItem);
        Assert.True(sw.ElapsedMilliseconds >= 500);

        sw.Restart();
        result = delayQueue.TryDequeue(out actualItem, TimeSpan.FromSeconds(1));
        sw.Stop();
        Assert.False(result);
        Assert.Null(actualItem);
        Assert.True(sw.ElapsedMilliseconds >= 1000);
    }

    [Fact]
    public void TryDequeue_ItemsNotEmpty()
    {
        // Arrange
        var delayQueue = new DelayQueue<TestDelayItem>();
        var now = DateTimeOffset.Now;
        var item1 = new TestDelayItem(GetTimespan(now, 10000));
        var item2 = new TestDelayItem(GetTimespan(now, -200));
        var item3 = new TestDelayItem(GetTimespan(now, 4000));
        var item4 = new TestDelayItem(GetTimespan(now, 5002));
        delayQueue.TryEnqueue(item1);
        delayQueue.TryEnqueue(item2);
        delayQueue.TryEnqueue(item3);
        delayQueue.TryEnqueue(item4);

        // Act and Assert
        TestDelayItem? actualItem;
        bool result = delayQueue.TryDequeue(out actualItem);
        Assert.True(result);
        Assert.Same(item2, actualItem);

        result = delayQueue.TryDequeue(out actualItem, 2000);
        Assert.False(result);

        result = delayQueue.TryDequeue(out actualItem, 5000);
        Assert.True(result);
        Assert.Same(item3, actualItem);
    }

    [Fact]
    public void Clear_ShouldBeEmpty()
    {
        // Arrange
        var delayQueue = new DelayQueue<TestDelayItem>();
        var now = DateTimeOffset.Now;
        delayQueue.TryEnqueue(new TestDelayItem(GetTimespan(now, 10000)));
        delayQueue.TryEnqueue(new TestDelayItem(GetTimespan(now, 11000)));
        delayQueue.TryEnqueue(new TestDelayItem(GetTimespan(now, 4000)));
        delayQueue.TryEnqueue(new TestDelayItem(GetTimespan(now, 5002)));

        // Act and Assert
        delayQueue.Clear();
        Assert.Equal(0, delayQueue.Count);
    }

    //多线程测试
    [Fact(Skip = "这个测试耗时40多秒，没必要每次都测试")]
    public async Task TestMultiThread()
    {
        var delayQueue = new DelayQueue<TestDelayItem<int>>();
        var now = DateTimeOffset.Now;

        // 一个线程，添加20个人物
        int taskCount = 20;
        _ = Task.Factory.StartNew(() =>
        {
            for (int i = 0; i < taskCount; i++)
            {
                var item = new TestDelayItem<int>(GetTimespan(now, i * 2000), i);
                delayQueue.TryEnqueue(item);
                if (i == 19)
                {
                    _output.WriteLine("最后一个元素过期时间为" + item.Expiration);
                }

            }
        }, TaskCreationOptions.LongRunning);


        // 10个线程来消费
        var outputs = new ConcurrentDictionary<int, int>();
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Factory.StartNew(() =>
            {
                while (delayQueue.Count > 0)
                {
                    if (delayQueue.TryDequeue(out var task, TimeSpan.FromSeconds(20)))
                    {
                        outputs.TryAdd(task.Data, Thread.CurrentThread.ManagedThreadId);
                    }
                }
            }, TaskCreationOptions.LongRunning));
        }

        await Task.WhenAll(tasks);

        Assert.Equal(0, delayQueue.Count);
        Assert.Equal(taskCount, outputs.Count);

        int preKey = -1;
        foreach (var output in outputs)
        {
            Assert.True(output.Key > preKey);
            preKey = output.Key;
        }

        // 打印每个线程消费的任务数量
        _output.WriteLine(string.Join(Environment.NewLine,
            outputs.GroupBy(o => o.Value).Select(g => $"线程: {g.Key}, 消费了: {g.Count()}")));
        _output.WriteLine("当前时间为" + DateTimeOffset.Now);
    }

    private long GetTimespan(DateTimeOffset source, long msOffset) => source.Add(TimeSpan.FromMilliseconds(msOffset)).ToUnixTimeMilliseconds();
}

internal class TestDelayItem : IDelayable<TestDelayItem>
{
    private readonly long _expiration;

    public TestDelayItem(long expiration)
    {
        _expiration = expiration;
        Expiration = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(_expiration), TimeZoneInfo.Local);
    }

    public DateTimeOffset Expiration { get; }

    public int CompareTo(TestDelayItem? other) => _expiration.CompareTo(other?._expiration ?? -1);

    public long GetDelay() => Math.Max(_expiration - DateTimeOffset.Now.ToUnixTimeMilliseconds(), 0);
}

internal class TestDelayItem<T> : TestDelayItem
{
    public TestDelayItem(long expiration, T data) : base(expiration)
    {
        Data = data;
    }

    public T Data { get; }
}
