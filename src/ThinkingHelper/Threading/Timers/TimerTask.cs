using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ThinkingHelper.Threading.Timers;

/// <summary>
/// 代表需要定时执行的任务行为
/// </summary>
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
/// 对TimerTask弱引用包装类型
/// 避免用户代码强引用TimerTask，导致TimerTaskEntry无法及时被垃圾回收
/// </summary>
public readonly struct TaskInfo : ITimerTask
{
    private readonly WeakReference<TimerTask> _weakReference;

    internal TaskInfo(TimerTask timerTask)
    {
        _weakReference = new WeakReference<TimerTask>(timerTask);
        DelayMs = timerTask.DelayMs;
    }

    /// <inheritdoc />
    public long DelayMs { get; }

    /// <inheritdoc />
    public void Cancel()
    {
        if (_weakReference.TryGetTarget(out var timerTask))
        {
            timerTask.Cancel();
        }
        //TimerTask被垃圾回收了，说明任务已执行或已取消，此时这里就什么都不用做
    }
}

/// <summary>
/// 代表需要定时执行的任务
/// </summary>
internal abstract class TimerTask : ITimerTask
{
    private TimerTaskEntry? _timerTaskEntry;

    protected TimerTask(long delayMs, bool repeat)
    {
        DelayMs = delayMs;
        Repeat = repeat;
    }

    /// <summary>
    /// 是否重复执行
    /// </summary>
    public bool Repeat { get; }

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

internal abstract class SyncTimerTask : TimerTask
{
    protected SyncTimerTask(long delayMs, bool repeat)
        : base(delayMs, repeat)
    {
    }

    internal abstract void Execute();
}

internal abstract class AsyncTimerTask : TimerTask
{
    protected AsyncTimerTask(long delayMs, bool repeat)
        : base(delayMs, repeat)
    {
    }

    internal abstract Task ExecuteAsync();
}

internal class SingleActionTimerTask : SyncTimerTask
{
    private readonly Action _delegate;

    public SingleActionTimerTask(Action task, long delayMs) : base(delayMs, false)
    {
        _delegate = task;
    }

    internal override void Execute() => _delegate.Invoke();
}

internal class SingleActionTimerTask<TState> : SyncTimerTask
{
    private readonly Action<TState?> _delegate;
    private readonly TState? _state;

    public SingleActionTimerTask(Action<TState?> task, TState? state, long delayMs) : base(delayMs, false)
    {
        _delegate = task;
        _state = state;
    }

    internal override void Execute() => _delegate.Invoke(_state);
}

internal class SingleFuncTimerTask : AsyncTimerTask
{
    private readonly Func<Task> _delegate;

    public SingleFuncTimerTask(Func<Task> task, long delayMs) : base(delayMs, false)
    {
        _delegate = task;
    }

    internal override Task ExecuteAsync() => _delegate.Invoke();
}

internal class SingleFuncTimerTask<TState> : AsyncTimerTask
{
    private readonly Func<TState?, Task> _delegate;
    private readonly TState? _state;

    public SingleFuncTimerTask(Func<TState?, Task> task, TState? state, long delayMs) : base(delayMs, false)
    {
        _delegate = task;
        _state = state;
    }

    internal override Task ExecuteAsync() => _delegate.Invoke(_state);
}

internal class MultiActionTimerTask : SyncTimerTask
{
    private readonly Action<TaskInfo> _delegate;
    private readonly TaskInfo _taskInfo;

    public MultiActionTimerTask(Action<TaskInfo> task, long delayMs) : base(delayMs, true)
    {
        _delegate = task;
        _taskInfo = new TaskInfo(this);
    }

    internal override void Execute() => _delegate.Invoke(_taskInfo);
}

internal class MultiActionTimerTask<TState> : SyncTimerTask
{
    private readonly Action<TaskInfo, TState?> _delegate;
    private readonly TState? _state;
    private readonly TaskInfo _taskInfo;

    public MultiActionTimerTask(Action<TaskInfo, TState?> task, TState? state, long delayMs) : base(delayMs, true)
    {
        _delegate = task;
        _state = state;
        _taskInfo = new TaskInfo(this);
    }

    internal override void Execute() => _delegate.Invoke(_taskInfo, _state);
}

internal class MultiFuncTimerTask : AsyncTimerTask
{
    private readonly Func<TaskInfo, Task> _delegate;
    private readonly TaskInfo _taskInfo;

    public MultiFuncTimerTask(Func<TaskInfo, Task> task, long delayMs) : base(delayMs, true)
    {
        _delegate = task;
        _taskInfo = new TaskInfo(this);
    }

    internal override Task ExecuteAsync() => _delegate.Invoke(_taskInfo);
}

internal class MultiFuncTimerTask<TState> : AsyncTimerTask
{
    private readonly Func<TaskInfo, TState?, Task> _delegate;
    private readonly TState? _state;
    private readonly TaskInfo _taskInfo;

    public MultiFuncTimerTask(Func<TaskInfo, TState?, Task> task, TState? state, long delayMs) : base(delayMs, true)
    {
        _delegate = task;
        _state = state;
        _taskInfo = new TaskInfo(this);
    }

    internal override Task ExecuteAsync() => _delegate.Invoke(_taskInfo, _state);
}
