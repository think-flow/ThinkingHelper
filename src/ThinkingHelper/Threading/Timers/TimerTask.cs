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
internal abstract class TimerTask : ITimerTask
{
    private TimerTaskEntry? _timerTaskEntry;

    protected TimerTask(long delayMs)
    {
        DelayMs = delayMs;
    }

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

internal class ActionTimerTask : TimerTask
{
    public ActionTimerTask(Action task, long delayMs)
        : base(delayMs)
    {
        Delegate = task;
    }

    public ActionTimerTask(Action task, TimeSpan delay)
        : this(task, (long) delay.TotalMilliseconds)
    {
    }

    public Action Delegate { get; }
}

internal class ActionTimerTaskWithState : TimerTask
{
    public ActionTimerTaskWithState(Action<object?> task, object? state, long delayMs)
        : base(delayMs)
    {
        Delegate = task;
        State = state;
    }

    public ActionTimerTaskWithState(Action<object?> task, object? state, TimeSpan delay)
        : this(task, state, (long) delay.TotalMilliseconds)
    {
    }

    public Action<object?> Delegate { get; }

    public object? State { get; }
}

internal class FuncAsyncTimerTask : TimerTask
{
    public FuncAsyncTimerTask(Func<Task> task, long delayMs)
        : base(delayMs)
    {
        Delegate = task;
    }

    public FuncAsyncTimerTask(Func<Task> task, TimeSpan delay)
        : this(task, (long) delay.TotalMilliseconds)
    {
    }

    public Func<Task> Delegate { get; }
}

internal class FuncAsyncTimerTaskWithState : TimerTask
{
    public FuncAsyncTimerTaskWithState(Func<object?, Task> task, object? state, long delayMs)
        : base(delayMs)
    {
        Delegate = task;
        State = state;
    }

    public FuncAsyncTimerTaskWithState(Func<object?, Task> task, object? state, TimeSpan delay)
        : this(task, state, (long) delay.TotalMilliseconds)
    {
    }

    public Func<object?, Task> Delegate { get; }

    public object? State { get; }
}
