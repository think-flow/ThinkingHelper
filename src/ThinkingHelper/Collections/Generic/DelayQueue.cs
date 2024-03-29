﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using ThinkingHelper.Threading;

namespace ThinkingHelper.Collections.Generic;

/// <summary>
/// 表示一个可以被延时队列接收的，延时项
/// </summary>
public interface IDelayable<in TElement> : IComparable<TElement>
{
    /// <summary>
    /// 返回元素剩余毫秒时间
    /// </summary>
    /// <returns></returns>
    long GetDelay();
}

/// <summary>
/// 表示一个延时队列 （该类使用锁，保证了线程安全）
/// </summary>
[Serializable]
[DebuggerDisplay("Count = {Count}")]
public class DelayQueue<TElement> where TElement : IDelayable<TElement>
{
    // ReSharper disable once StaticMemberInGenericType
    [ThreadStatic]
    private static Stopwatch? _stopwatch;

    private readonly object _lock = new object();
    private readonly PriorityQueue<TElement, TElement> _queue; //使用优先队列作为底层数据结构
    private Thread? _leader; //等待线程标识

    /// <summary>
    /// </summary>
    public DelayQueue() : this(0)
    {
    }

    /// <summary>
    /// </summary>
    /// <param name="initialCapacity">底层优先队列的初始容量</param>
    public DelayQueue(int initialCapacity)
    {
        _queue = new PriorityQueue<TElement, TElement>(initialCapacity);
    }

    /// <summary>
    /// 获取包含的元素数量
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _queue.Count;
            }
        }
    }

    /// <summary>
    /// 入队，该方法非阻塞
    /// </summary>
    public bool TryEnqueue(TElement element)
    {
        bool acquiredLock = false;
        Monitor.Enter(_lock, ref acquiredLock);
        try
        {
            _queue.Enqueue(element, element);

            if (ReferenceEquals(_queue.Peek(), element))
            {
                //如果队列头是当前元素，说明当前元素优先级最小，即将过期。此时通知其他线程
                _leader = null;
                Monitor.Pulse(_lock);
            }

            return true;
        }
        finally
        {
            if (acquiredLock)
            {
                Monitor.Exit(_lock);
            }
        }
    }

    /// <summary>
    /// 获取队列的头部元素，并在指定的等待时间前阻塞
    /// </summary>
    public bool TryDequeue([MaybeNullWhen(false)] out TElement item, TimeSpan timeout)
    {
        long millisecondsTimeout = timeout.TotalMilliseconds <= long.MaxValue
            ? (long) timeout.TotalMilliseconds
            : Timeout.Infinite; //如果设置的超时时间大于long的最大值了，对于人类而言，相当于无限了
        return TryDequeue(out item, millisecondsTimeout);
    }

    /// <summary>
    /// 获取队列的头部元素，将不会阻塞等待。
    /// </summary>
    public bool TryDequeue([MaybeNullWhen(false)] out TElement item) => TryDequeue(out item, 0L);

    /// <summary>
    /// 获取队列的头部元素，并在指定的等待时间前阻塞
    /// </summary>
    public bool TryDequeue([MaybeNullWhen(false)] out TElement item, long millisecondsTimeout)
    {
        item = default;
        long timeout = millisecondsTimeout;

        bool lockTaken = false;
        timeout = MonitorTryEnter(_lock, timeout, ref lockTaken);

        if (!lockTaken) return false;

        try
        {
            while (true)
            {
                //队列为空
                if (!_queue.TryPeek(out var first, out _))
                {
                    //达到超时时间
                    if (IsTimeout(timeout))
                    {
                        return false;
                    }

                    //进入等待队列进行等待
                    timeout = MonitorWait(_lock, timeout);
                }
                else
                {
                    long delay = first.GetDelay();
                    //元素已经到期
                    if (delay <= 0)
                    {
                        item = _queue.Dequeue();
                        return true;
                    }

                    //元素未到期，但达到超时时间
                    if (IsTimeout(timeout))
                    {
                        return false;
                    }

                    if ((timeout < delay && timeout != Timeout.Infinite)
                        || _leader != null)
                    {
                        //_leader的作用在于，如果有一个线程在等待数据，说明还没有数据到期。其他线程没必要抢占锁资源。继续等待即可
                        //超时时间小于延迟时间 或者有其他线程在等待数据，那么当前线程也等待
                        timeout = MonitorWait(_lock, timeout);
                    }
                    else
                    {
                        //超时时间大于延迟时间 且 没有其他线程在等待
                        var currentThread = Thread.CurrentThread;
                        _leader = currentThread;
                        try
                        {
                            long timeLeft = MonitorWait(_lock, delay);
                            timeout -= delay - timeLeft; //delay - timeLeft 求出MonitorWait消耗的时间
                        }
                        finally
                        {
                            if (_leader == currentThread)
                            {
                                _leader = null;
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }

    /// <summary>
    /// 获取队列的头部元素，但不移除
    /// </summary>
    public bool TryPeek([MaybeNullWhen(false)] out TElement item)
    {
        lock (_lock)
        {
            return _queue.TryPeek(out item, out var _);
        }
    }

    /// <summary>
    /// 清空队列
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _queue.Clear();
        }
    }

    /// <summary>
    /// Sets the capacity to the actual number of items in the underlying <see cref="PriorityQueue{TElement, TPriority}" />,
    /// if that is less than 90 percent of current capacity.
    /// </summary>
    /// <remarks>
    /// This method can be used to minimize a collection's memory overhead
    /// if no new elements will be added to the collection.
    /// </remarks>
    public void TrimExcess()
    {
        lock (_lock)
        {
            _queue.TrimExcess();
        }
    }

    /// <summary>
    /// 获取当前队列的快照。快照不保证数据的有序性
    /// </summary>
    public IReadOnlyCollection<TElement> Snapshot()
    {
        lock (_lock)
        {
            return _queue.UnorderedItems.Select(i => i.Element).ToList();
        }
    }

    //判断给定的超时时间，是否达到超时条件
    private static bool IsTimeout(long millisecondsTimeout) => millisecondsTimeout <= 0 && millisecondsTimeout != Timeout.Infinite;

    private long MonitorWait(object obj, long timeout)
    {
        //如果超时时间设为无限，则剩余超时时间也为无限
        if (timeout == Timeout.Infinite)
        {
            LongMonitor.Wait(obj, timeout);
            return Timeout.Infinite;
        }

        _stopwatch ??= new Stopwatch();
        var sw = _stopwatch;
        sw.Restart();
        LongMonitor.Wait(obj, timeout);
        sw.Stop();
        long left = timeout - sw.ElapsedMilliseconds;
        if (left == Timeout.Infinite)
        {
            //排除计算出代表无限的-1值
            left = 0;
        }

        return left;
    }

    private long MonitorTryEnter(object obj, long timeout, ref bool lockTaken)
    {
        //如果超时时间设为无限，则剩余超时时间也为无限
        if (timeout == Timeout.Infinite)
        {
            LongMonitor.TryEnter(obj, timeout, ref lockTaken);
            return Timeout.Infinite;
        }

        _stopwatch ??= new Stopwatch();
        var sw = _stopwatch;
        sw.Restart();
        LongMonitor.TryEnter(obj, timeout, ref lockTaken);
        sw.Stop();
        long left = timeout - sw.ElapsedMilliseconds;
        if (left == Timeout.Infinite)
        {
            //排除计算出代表无限的-1值
            left = 0;
        }

        return left;
    }
}
