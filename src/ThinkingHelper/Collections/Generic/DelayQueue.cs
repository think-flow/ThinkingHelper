using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

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
        item = default;
        var sw = new Stopwatch();

        if (Monitor.TryEnter(_lock, timeout))
        {
            try
            {
                while (true)
                {
                    //队列为空
                    if (!_queue.TryPeek(out var first, out var _))
                    {
                        //达到超时时间
                        if (IsTimeout(timeout))
                        {
                            return false;
                        }

                        //进入等待队列进行等待
                        timeout = MonitorWait(sw, _lock, timeout);
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

                        if ((timeout.TotalMilliseconds < delay && timeout != Timeout.InfiniteTimeSpan)
                            || _leader != null)
                        {
                            //_leader的作用在于，如果有一个线程在等待数据，说明还没有数据到期。其他线程没必要抢占锁资源。继续等待即可
                            //超时时间小于延迟时间 或者有其他线程在等待数据，那么当前线程也等待
                            timeout = MonitorWait(sw, _lock, timeout);
                        }
                        else
                        {
                            //超时时间大于延迟时间 且 没有其他线程在等待
                            var currentThread = Thread.CurrentThread;
                            _leader = currentThread;
                            try
                            {
                                var timeLeft = MonitorWait(sw, _lock, TimeSpan.FromMilliseconds(delay));
                                timeout -= TimeSpan.FromMilliseconds(delay - timeLeft.TotalMilliseconds);
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

        return false;

        static bool IsTimeout(TimeSpan timeSpan)
        {
            return timeSpan <= TimeSpan.Zero && timeSpan != Timeout.InfiniteTimeSpan;
        }

        static TimeSpan MonitorWait(Stopwatch sw, object obj, TimeSpan timeout)
        {
            //如果超时时间设为无限，则剩余超时时间也为无限
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                Monitor.Wait(obj, timeout);
                return timeout;
            }

            sw.Restart();
            Monitor.Wait(obj, timeout);
            sw.Stop();
            var left = timeout - sw.Elapsed;
            if (left == Timeout.InfiniteTimeSpan)
            {
                //排除计算出代表无限的-1值
                left = TimeSpan.Zero;
            }

            return left;
        }
    }

    /// <summary>
    /// 获取队列的头部元素，将不会阻塞等待。
    /// </summary>
    public bool TryDequeue([MaybeNullWhen(false)] out TElement item) => TryDequeue(out item, TimeSpan.Zero);

    /// <summary>
    /// 获取队列的头部元素，并在指定的等待时间前阻塞
    /// </summary>
    public bool TryDequeue([MaybeNullWhen(false)] out TElement item, long millisecondsTimeout) => TryDequeue(out item, TimeSpan.FromMilliseconds(millisecondsTimeout));

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
}
