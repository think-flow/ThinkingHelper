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

    public void Execute(object? obj) => ExecuteCore((SyncTimerTask) obj!);

    protected abstract void ExecuteCore(SyncTimerTask task);
}

internal abstract class AsyncTimerTask : TimerTask
{
    protected AsyncTimerTask(long delayMs, bool repeat)
        : base(delayMs, repeat)
    {
    }

    public Task ExecuteAsync(object? obj) => ExecuteCore((AsyncTimerTask) obj!);

    protected abstract Task ExecuteCore(AsyncTimerTask task);
}

internal class SingleActionTimerTask : SyncTimerTask
{
    private readonly Action _delegate;

    public SingleActionTimerTask(Action task, long delayMs) : base(delayMs, false)
    {
        _delegate = task;
    }

    protected override void ExecuteCore(SyncTimerTask task) => _delegate.Invoke();
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

    protected override void ExecuteCore(SyncTimerTask task) => _delegate.Invoke(_state);
}

internal class SingleFuncTimerTask : AsyncTimerTask
{
    private readonly Func<Task> _delegate;

    public SingleFuncTimerTask(Func<Task> task, long delayMs) : base(delayMs, false)
    {
        _delegate = task;
    }

    protected override Task ExecuteCore(AsyncTimerTask task) => _delegate.Invoke();
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

    protected override Task ExecuteCore(AsyncTimerTask task) => _delegate.Invoke(_state);
}

internal class MultiActionTimerTask : SyncTimerTask
{
    private readonly Action<ITimerTask> _delegate;

    public MultiActionTimerTask(Action<ITimerTask> task, long delayMs) : base(delayMs, true)
    {
        _delegate = task;
    }

    protected override void ExecuteCore(SyncTimerTask task) => _delegate.Invoke(task);
}

internal class MultiActionTimerTask<TState> : SyncTimerTask
{
    private readonly Action<ITimerTask, TState?> _delegate;
    private readonly TState? _state;

    public MultiActionTimerTask(Action<ITimerTask, TState?> task, TState? state, long delayMs) : base(delayMs, true)
    {
        _delegate = task;
        _state = state;
    }

    protected override void ExecuteCore(SyncTimerTask task) => _delegate.Invoke(task, _state);
}

internal class MultiFuncTimerTask : AsyncTimerTask
{
    private readonly Func<ITimerTask, Task> _delegate;

    public MultiFuncTimerTask(Func<ITimerTask, Task> task, long delayMs) : base(delayMs, true)
    {
        _delegate = task;
    }

    protected override Task ExecuteCore(AsyncTimerTask task) => _delegate.Invoke(task);
}

internal class MultiFuncTimerTask<TState> : AsyncTimerTask
{
    private readonly Func<ITimerTask, TState?, Task> _delegate;
    private readonly TState? _state;

    public MultiFuncTimerTask(Func<ITimerTask, TState?, Task> task, TState? state, long delayMs) : base(delayMs, true)
    {
        _delegate = task;
        _state = state;
    }

    protected override Task ExecuteCore(AsyncTimerTask task) => _delegate.Invoke(task, _state);
}
