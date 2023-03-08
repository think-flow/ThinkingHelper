using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ThinkingHelper.Collections.Generic;

namespace ThinkingHelper.Threading.Timers;

/// <summary>
/// 基于时间轮的任务定时器
/// </summary>
public sealed class TaskTimer : IDisposable
{
    private readonly DelayQueue<TimerTaskList> _delayQueue;
    private readonly ReaderWriterLockSlim _lock;
    private readonly AtomicInt _taskCounter;
    private readonly TimingWheel _timingWheel;
    private readonly CancellationTokenSource _tokenSource;
    private bool _disposed;

    /// <summary>
    /// </summary>
    /// <param name="tickMs">时间槽精度，单位毫秒</param>
    /// <param name="wheelSize">时间槽数量</param>
    /// <param name="startMs">时间轮起始时间。毫秒时间戳。为null时使用当前unix毫秒时间戳</param>
    public TaskTimer(long tickMs = 1, int wheelSize = 20, long? startMs = null)
    {
        if (startMs.HasValue && startMs.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startMs), startMs.Value, "The value must be greater than 0");
        }

        _taskCounter = new AtomicInt(0);
        _delayQueue = new DelayQueue<TimerTaskList>();
        startMs ??= DateTimeOffset.Now.ToUnixTimeMilliseconds();
        _timingWheel = new TimingWheel(tickMs, wheelSize, startMs.Value, _taskCounter, _delayQueue);
        _lock = new ReaderWriterLockSlim();
        _tokenSource = new CancellationTokenSource();
        RunTimer(_tokenSource.Token);
    }

    /// <summary>
    /// 定时器中的任务数
    /// </summary>
    public int Count
    {
        get
        {
            ThrowIfDisposed();
            return _taskCounter.Get();
        }
    }

    /// <summary>
    /// 关闭定时器，并释放所有资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _lock.Dispose();
        _tokenSource.Cancel();
        _tokenSource.Dispose();

        _disposed = true;
    }

    /// <summary>
    /// 添加定时任务
    /// </summary>
    private ITimerTask Add(TimerTask task)
    {
        ThrowIfDisposed();
        _lock.EnterReadLock();
        try
        {
            //添加的任务绝对过期时间，将是用时间轮中记录的时间+设置的延时时间决定
            //所以如果时间轮时间小于当前时间，那个如果加上延时时间，也不大于当前时间，那么任务将立即过期并在tickMsg后执行
            AddTimerTaskEntry(new TimerTaskEntry(task, task.DelayMs + DateTimeOffset.Now.ToUnixTimeMilliseconds()));
            return task;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public ITimerTask Add(Action task, long timeoutMs) => Add(new ActionTimerTask(task, timeoutMs));

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public ITimerTask Add(Action task, TimeSpan timeout) => Add(task, (long) timeout.TotalMilliseconds);

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public ITimerTask Add(Action<object?> task, object? state, long timeoutMs) => Add(new ActionTimerTaskWithState(task, state, timeoutMs));

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public ITimerTask Add(Action<object?> task, object? state, TimeSpan timeout) => Add(task, state, (long) timeout.TotalMilliseconds);

    /// <summary>
    /// 关闭定时器，并释放所有资源
    /// </summary>
    public void Shutdown() => Dispose();

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public ITimerTask Add(Func<Task> task, long timeoutMs) => Add(new FuncAsyncTimerTask(task, timeoutMs));

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public ITimerTask Add(Func<Task> task, TimeSpan timeout) => Add(task, (long) timeout.TotalMilliseconds);

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public ITimerTask Add(Func<object?, Task> task, object? state, long timeoutMs) => Add(new FuncAsyncTimerTaskWithState(task, state, timeoutMs));

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public ITimerTask Add(Func<object?, Task> task, object? state, TimeSpan timeout) => Add(task, state, (long) timeout.TotalMilliseconds);

    private void AddTimerTaskEntry(TimerTaskEntry entry)
    {
        if (_timingWheel.Add(entry)) return;

        //未能添加到时间轮中，这说明任务处于已过期或者已取消状态
        //任务取消，则不执行任务
        if (entry.Cancelled) return;

        //任务过期，立即执行相关任务
        var timerTask = entry.TimerTask;
        Debug.Assert(timerTask is not null);
        switch (timerTask)
        {
            case ActionTimerTask task:
                Task.Factory.StartNew(task.Delegate, _tokenSource.Token,
                    TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                //如果想要捕捉到任务中出现的未捕获异常，那么可以通过ContinueWith,来捕获未处理异常，并通过回调或者事件方式，将异常传递给用户注册的异常处理器
                // Task.Factory.StartNew(task.Delegate, _tokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default)
                //     .Unwrap()
                //     .ContinueWith(t => _exceptionHandler?.Invoke(t.Exception!.GetBaseException()), TaskContinuationOptions.OnlyOnFaulted);
                break;
            case ActionTimerTaskWithState task:
                Task.Factory.StartNew(task.Delegate, task.State, _tokenSource.Token,
                    TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                break;
            case FuncAsyncTimerTask task:
                Task.Factory.StartNew(task.Delegate, _tokenSource.Token,
                    TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                break;
            case FuncAsyncTimerTaskWithState task:
                Task.Factory.StartNew(task.Delegate, task.State, _tokenSource.Token,
                    TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                break;
            default:
                throw new InvalidOperationException("unknown TimerTask derived type");
        }
    }

    //推进时间轮
    private bool AdvanceClock(long timeoutMs, CancellationToken cancellationToken)
    {
        //timeout设置-1，防止时间轮指针空转
        if (_delayQueue.TryDequeue(out var bucket, timeoutMs))
        {
            cancellationToken.ThrowIfCancellationRequested();
            _lock.EnterWriteLock();
            try
            {
                do
                {
                    //时间轮的推进值，设定为延时队列中获取的元素的绝对过期时间
                    _timingWheel.AdvanceClock(bucket.GetExpiration());
                    // 将溢出时间轮插入到低层的时间轮中
                    bucket.Flush(AddTimerTaskEntry);
                    //使用无阻塞方式，获取 将溢出时间轮插入到低层的时间轮中后可能存在的数据
                } while (_delayQueue.TryDequeue(out bucket));
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return true;
        }

        return false;
    }

    //启动定时器线程
    private void RunTimer(CancellationToken cancellationToken)
    {
        //开启一个新线程，用来推进时间轮
        new Thread(RunTimerCore)
        {
            IsBackground = true,
            Name = "TimingWheelTimer"
        }.Start();

        void RunTimerCore()
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    AdvanceClock(Timeout.Infinite, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(GetType().Name);
    }
}
