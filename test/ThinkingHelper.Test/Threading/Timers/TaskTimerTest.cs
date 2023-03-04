using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ThinkingHelper.Threading.Timers;
using Xunit;
using Xunit.Abstractions;

namespace ThinkingHelper.Test.Threading.Timers;

public class TaskTimerTest
{
    private readonly ITestOutputHelper _output;

    public TaskTimerTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Count_ShouldSuccess()
    {
        // Arrange
        var dateTime = DateTimeOffset.Now;
        var timer = new TaskTimer(100, 60, dateTime.ToUnixTimeMilliseconds());
        _output.WriteLine($"启动时间为{DateTimeOffset.Now:yyyy/MM/dd HH:mm:ss.fff}");
        int startMs = 1000;
        for (int i = 0; i < 4; i++)
        {
            int j = i;
            timer.Add(() =>
            {
                _output.WriteLine($"{j} thread Id {Thread.CurrentThread.ManagedThreadId} {DateTimeOffset.Now:yyyy/MM/dd HH:mm:ss.fff}");
            }, startMs);
            startMs += 500;
        }

        // Act and Assert
        Assert.Equal(4, timer.Count);
        await Task.Delay(1600);
        Assert.Equal(2, timer.Count);
    }

    [Fact]
    public void Dispose_ThrowWhenDisposed()
    {
        // Arrange
        var timer = new TaskTimer(100, 60, DateTimeOffset.Now.ToUnixTimeMilliseconds());

        // Act and Assert
        timer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => timer.Add(() =>
        {
        }, 100));

        Assert.Throws<ObjectDisposedException>(() => timer.Count);
    }

    [Fact]
    public async Task Dispose_TimerServerShoutShutDown()
    {
        // Arrange
        var timer = new TaskTimer(100, 60, DateTimeOffset.Now.ToUnixTimeMilliseconds());
        int result = 0;
        timer.Add(() =>
        {
            result = 100;
        }, 100);

        // Act and Assert
        Assert.Equal(0, result);
        timer.Dispose();
        await Task.Delay(200);

        Assert.Equal(0, result);
    }

    [Fact]
    public void Add_ZeroDelay_ShouldExecuteImmediately()
    {
        // Arrange
        var dateTime = DateTimeOffset.Now;
        var timer = new TaskTimer(100, 60, dateTime.ToUnixTimeMilliseconds());

        // Act and Assert
        var sw = Stopwatch.StartNew();
        var ev = new ManualResetEventSlim();
        timer.Add(() =>
        {
            sw.Stop();
            ev.Set();
        }, 0);
        ev.Wait();
        Assert.True(sw.ElapsedMilliseconds < 10);

        ev.Reset();
        sw.Restart();
        timer.Add(() =>
        {
            sw.Stop();
            ev.Set();
        }, -1);
        ev.Wait();
        Assert.True(sw.ElapsedMilliseconds < 10);
    }

    [Fact]
    public void Add_AsyncTask_ShouldSuccess()
    {
        // Arrange
        var dateTime = DateTimeOffset.Now;
        var timer = new TaskTimer(10, 60, dateTime.ToUnixTimeMilliseconds());

        // Act and Assert
        var sw = Stopwatch.StartNew();
        var ev = new ManualResetEventSlim();
        timer.Add(async () =>
        {
            await Task.Delay(300);
            sw.Stop();
            ev.Set();
        }, 100);
        ev.Wait();
        _output.WriteLine(sw.ElapsedMilliseconds.ToString());
        Assert.True(sw.ElapsedMilliseconds < 450);
    }

    [Fact]
    public async Task Executing_TaskThrowException_TimerNotThrow()
    {
        // Arrange
        var dateTime = DateTimeOffset.Now;
        var timer = new TaskTimer(100, 60, dateTime.ToUnixTimeMilliseconds());

        // Act and Assert
        int result = 10;
        timer.Add(() => throw new Exception(), 200);
        Assert.Equal(10, result);
        await Task.Delay(300);

        timer.Add(async () =>
        {
            try
            {
                await Task.Delay(50);
                throw new Exception();
            }
            catch (Exception)
            {
                result = 11;
            }
        }, 200);
        await Task.Delay(300);
        Assert.Equal(11, result);
    }

    //todo 测试大量任务下的执行精度

    [Fact]
    public async Task Cancel_TaskNotExecuted()
    {
        // Arrange
        var dateTime = DateTimeOffset.Now;
        var timer = new TaskTimer(1, 60, dateTime.ToUnixTimeMilliseconds());

        // Act and Assert
        int result = 100;
        var timerTask = timer.Add(() =>
        {
            result = 0;
        }, 100);
        timerTask.Cancel();
        await Task.Delay(200);
        Assert.Equal(100, result);
    }
}
