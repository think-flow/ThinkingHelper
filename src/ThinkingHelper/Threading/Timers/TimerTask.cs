using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ThinkingHelper.Threading.Timers;

public interface ITimerTask
{
    /// <summary>
    /// 该任务的延时时间
    /// </summary>
    public long DelayMs { get; }

    /// <summary>
    /// 通知TimerServer应该取消执行该任务
    /// </summary>
    public void Cancel();
}

/// <summary>
/// 代表需要定时执行的任务
/// </summary>
internal class TimerTask : ITimerTask
{
    private TimerTaskEntry? _timerTaskEntry;

    public TimerTask(Action task, TimeSpan delay)
        : this(task, (long) delay.TotalMilliseconds)
    {
    }

    public TimerTask(Action task, long delayMs)
    {
        Delegate = task;
        IsAsync = false;
        DelayMs = delayMs;
    }

    public TimerTask(Func<Task> task, TimeSpan delay)
        : this(task, (long) delay.TotalMilliseconds)
    {
    }

    public TimerTask(Func<Task> task, long delayMs)
    {
        Delegate = task;
        IsAsync = true;
        DelayMs = delayMs;
    }

    /// <summary>
    /// 需要执行的elegate
    /// </summary>
    public Delegate Delegate { get; }

    /// <summary>
    /// 是否是异步方法
    /// </summary>
    public bool IsAsync { get; }

    /// <inheritdoc />
    public long DelayMs { get; }

    /// <inheritdoc />
    public void Cancel()
    {
        var entry = GetTimerTaskEntry();
        Debug.Assert(entry is not null);
        entry.TimerTask = null;
    }

    public TimerTaskEntry? GetTimerTaskEntry() => _timerTaskEntry;

    public void SetTimerTaskEntry(TimerTaskEntry entry)
    {
        lock (this)
        {
            // if this timerTask is already held by an existing timer task entry,
            // we will remove such an entry first.
            if (_timerTaskEntry != null && _timerTaskEntry != entry)
            {
                entry.Remove();
            }

            _timerTaskEntry = entry;
        }
    }
}
