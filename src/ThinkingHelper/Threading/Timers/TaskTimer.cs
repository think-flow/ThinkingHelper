using System;
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
        RunTimer();
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

        _tokenSource.Cancel();
        _tokenSource.Dispose();
        _lock.Dispose();

        _disposed = true;
    }

    //添加定时任务
    private TaskInfo Add(TimerTask task)
    {
        ThrowIfDisposed();
        _lock.EnterReadLock();
        try
        {
            //添加的任务绝对过期时间，将是用时间轮中记录的时间+设置的延时时间决定
            //所以如果时间轮时间小于当前时间，那个如果加上延时时间，也不大于当前时间，那么任务将立即过期并在tickMsg后执行
            AddTimerTaskEntry(new TimerTaskEntry(task, task.DelayMs + DateTimeOffset.Now.ToUnixTimeMilliseconds()));
            return new TaskInfo(task);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    //将需要重复执行的任务，重新添加到时间轮中
    private void UnsafeRepeatAdd(TimerTask oldTask)
    {
        //这里没有加锁，所以需要保证调用该方法的地方，必须已获得_lock的读写锁
        var taskEntry = new TimerTaskEntry(oldTask, oldTask.DelayMs + oldTask.GetTimerTaskEntry()!.ExpirationMs);
        AddTimerTaskEntry(taskEntry);
    }

    /// <summary>
    /// 关闭定时器，并释放所有资源
    /// </summary>
    public void Shutdown() => Dispose();

    //将任务添加到时间轮中。并负责执行到期的任务
    private void AddTimerTaskEntry(TimerTaskEntry entry)
    {
        if (_timingWheel.Add(entry)) return;

        //未能添加到时间轮中，这说明任务处于已过期或者已取消状态
        //任务取消，则不执行任务
        if (entry.Cancelled) return;

        //任务过期，立即执行相关任务
        var timerTask = entry.TimerTask;
        if (timerTask is null) return;

        switch (timerTask)
        {
            case SyncTimerTask task:
                Task.Factory.StartNew(task.Execute, _tokenSource.Token,
                    TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                break;
            case AsyncTimerTask task:
                //如果想要捕捉到任务中出现的未捕获异常，那么可以通过ContinueWith,来捕获未处理异常，并通过回调或者事件方式，将异常传递给用户注册的异常处理器
                // Task.Factory.StartNew(task.ExecuteAsync, _tokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default)
                //     .Unwrap()
                //     .ContinueWith(t => _exceptionHandler?.Invoke(t.Exception!.GetBaseException()), TaskContinuationOptions.OnlyOnFaulted);
                Task.Factory.StartNew(task.ExecuteAsync, _tokenSource.Token,
                    TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                break;
            default:
                throw new InvalidOperationException("unknown TimerTask derived type");
        }

        //需要重复执行的任务，将继续添加到时间轮中
        if (timerTask.Repeat)
        {
            UnsafeRepeatAdd(timerTask);
        }
    }

    //推进时间轮
    private bool AdvanceClock(long timeoutMs)
    {
        //timeout设置-1，防止时间轮指针空转
        if (_delayQueue.TryDequeue(out var bucket, timeoutMs))
        {
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
                } while (!_tokenSource.IsCancellationRequested && _delayQueue.TryDequeue(out bucket));
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
    private void RunTimer()
    {
        //开启一个新线程，用来推进时间轮
        var thread = new Thread(RunTimerCore)
        {
            IsBackground = true,
            Name = "TimingWheelTimer"
        };
        thread.Start(thread);

        void RunTimerCore(object? state)
        {
            var timingWheelTimerThread = (Thread) state!;
            //注册CancellationToken取消回调
            //通过线程的Interrupt方法，中断可能处于阻塞状态的timer线程。已便使线程正常退出
            _tokenSource.Token.Register(timingWheelTimerThread.Interrupt);

            while (!_tokenSource.IsCancellationRequested)
            {
                try
                {
                    AdvanceClock(Timeout.Infinite);
                }
                catch (ThreadInterruptedException)
                {
                    //捕获线程中断异常，并让线程跳出循环正常结束
                    break;
                }
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(GetType().Name);
    }

    #region 添加任务

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public TaskInfo Add(Action task, long timeoutMs)
        => Add(new SingleActionTimerTask(task, timeoutMs));

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public TaskInfo Add<TState>(Action<TState?> task, TState? state, long timeoutMs)
        => Add(new SingleActionTimerTask<TState>(task, state, timeoutMs));

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public TaskInfo Add(Func<Task> task, long timeoutMs)
        => Add(new SingleFuncTimerTask(task, timeoutMs));

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public TaskInfo Add<TState>(Func<TState?, Task> task, TState? state, long timeoutMs)
        => Add(new SingleFuncTimerTask<TState>(task, state, timeoutMs));

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public TaskInfo Add(Action task, TimeSpan timeout)
        => Add(task, (long) timeout.TotalMilliseconds);

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public TaskInfo Add<TState>(Action<TState?> task, TState? state, TimeSpan timeout)
        => Add(task, state, (long) timeout.TotalMilliseconds);

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public TaskInfo Add(Func<Task> task, TimeSpan timeout)
        => Add(task, (long) timeout.TotalMilliseconds);

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public TaskInfo Add<TState>(Func<TState?, Task> task, TState? state, TimeSpan timeout)
        => Add(task, state, (long) timeout.TotalMilliseconds);

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public TaskInfo AddRepeat(Action<TaskInfo> task, long timeoutMs)
        => Add(new MultiActionTimerTask(task, timeoutMs));

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public TaskInfo AddRepeat<TState>(Action<TaskInfo, TState?> task, TState? state, long timeoutMs)
        => Add(new MultiActionTimerTask<TState>(task, state, timeoutMs));

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public TaskInfo AddRepeat(Func<TaskInfo, Task> task, long timeoutMs)
        => Add(new MultiFuncTimerTask(task, timeoutMs));

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public TaskInfo AddRepeat<TState>(Func<TaskInfo, TState?, Task> task, TState? state, long timeoutMs)
        => Add(new MultiFuncTimerTask<TState>(task, state, timeoutMs));

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public TaskInfo AddRepeat(Action<TaskInfo> task, TimeSpan timeout)
        => AddRepeat(task, (long) timeout.TotalMilliseconds);

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public TaskInfo AddRepeat<TState>(Action<TaskInfo, TState?> task, TState? state, TimeSpan timeout)
        => AddRepeat(task, state, (long) timeout.TotalMilliseconds);

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public TaskInfo AddRepeat(Func<TaskInfo, Task> task, TimeSpan timeout)
        => AddRepeat(task, (long) timeout.TotalMilliseconds);

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public TaskInfo AddRepeat<TState>(Func<TaskInfo, TState?, Task> task, TState? state, TimeSpan timeout)
        => AddRepeat(task, state, (long) timeout.TotalMilliseconds);

    #endregion
}
