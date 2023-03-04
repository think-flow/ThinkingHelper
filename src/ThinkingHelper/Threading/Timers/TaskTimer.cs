using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ThinkingHelper.Collections.Generic;

namespace ThinkingHelper.Threading.Timers;

/// <summary>
/// 基于时间轮的任务定时器
/// </summary>
public sealed partial class TaskTimer : IDisposable
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
    /// <param name="startMs">起始时间。毫秒时间戳</param>
    /// <example>
    /// 时间槽精度请按需设置。例如设为1000毫秒，如果任务延时500毫秒，那么任务将立即运行。而如果延时设为1400，那么任务将在1000毫秒后立即运行，而不是1400毫秒运行。
    /// </example>
    public TaskTimer(long tickMs, int wheelSize, long startMs)
    {
        _taskCounter = new AtomicInt(0);
        _delayQueue = new DelayQueue<TimerTaskList>();
        _timingWheel = new TimingWheel(tickMs, wheelSize, startMs, _taskCounter, _delayQueue);
        _lock = new ReaderWriterLockSlim();
        _tokenSource = new CancellationTokenSource();
        RunTimer(tickMs, _tokenSource.Token);
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
    /// 添加定时任务
    /// </summary>
    private ITimerTask Add(TimerTask task)
    {
        ThrowIfDisposed();
        _lock.EnterReadLock();
        try
        {
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
    public ITimerTask Add(Action task, long timeoutMs) => Add(new TimerTask(task, timeoutMs));

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public ITimerTask Add(Action task, TimeSpan timeout) => Add(task, (long) timeout.TotalMilliseconds);

    /// <summary>
    /// 关闭定时器，并释放所有资源
    /// </summary>
    public void Shutdown() => Dispose();

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public ITimerTask Add(Func<Task> task, long timeoutMs) => Add(new TimerTask(task, timeoutMs));

    /// <summary>
    /// 添加定时任务
    /// </summary>
    public ITimerTask Add(Func<Task> task, TimeSpan timeout) => Add(task, (long) timeout.TotalMilliseconds);

    private void AddTimerTaskEntry(TimerTaskEntry entry)
    {
        if (!_timingWheel.Add(entry))
        {
            //已经过期或者取消
            if (!entry.Cancelled)
            {
                //一定是已过期状态，立即执行相关任务
                var timerTask = entry.TimerTask;
                Debug.Assert(timerTask is not null);
                if (timerTask.IsAsync)
                {
                    Task.Factory.StartNew((Func<Task>) timerTask.Delegate, _tokenSource.Token,
                        TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

                    //如果想要捕捉到任务中出现的未捕获异常，那么可以通过ContinueWith,来捕获未处理异常，并通过回调或者事件方式，将异常传递给用户注册的异常处理器
                    //Task.Factory.StartNew((Func<Task>) timerTask.Delegate, _tokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap().ContinueWith(t => _exceptionHandler?.Invoke(t.Exception!.GetBaseException()), TaskContinuationOptions.OnlyOnFaulted);
                }
                else
                {
                    Task.Factory.StartNew((Action) timerTask.Delegate, _tokenSource.Token,
                        TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

                    //Task.Factory.StartNew((Action) timerTask.Delegate, _tokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).ContinueWith(t => _exceptionHandler?.Invoke(t.Exception!.GetBaseException()), TaskContinuationOptions.OnlyOnFaulted);
                }
            }
        }
    }

    //推进时间轮
    private bool AdvanceClock(long timeoutMs, CancellationToken cancellationToken)
    {
        if (_delayQueue.TryDequeue(out var bucket, timeoutMs))
        {
            cancellationToken.ThrowIfCancellationRequested();
            _lock.EnterWriteLock();
            try
            {
                do
                {
                    _timingWheel.AdvanceClock(bucket.GetExpiration());
                    bucket.Flush(AddTimerTaskEntry);
                } while (_delayQueue.TryDequeue(out bucket, timeoutMs));
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
    private void RunTimer(long tickMs, CancellationToken cancellationToken)
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
                    AdvanceClock(tickMs, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}

public partial class TaskTimer
{
    /// <summary>
    /// 关闭定时器，并释放所有资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _lock.Dispose();
        _tokenSource.Cancel();
        _tokenSource.Dispose();
        //通知所有的TimerTask取消


        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(GetType().Name);
    }
}
