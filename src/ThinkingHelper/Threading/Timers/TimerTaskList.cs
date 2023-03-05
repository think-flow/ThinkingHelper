using System;
using ThinkingHelper.Collections.Generic;

namespace ThinkingHelper.Threading.Timers;

//该数据结构为双向循环链表
internal class TimerTaskList : IDelayable<TimerTaskList>
{
    private readonly AtomicLong _expiration;
    private readonly TimerTaskEntry _root;
    private readonly AtomicInt _taskCounter;

    public TimerTaskList(AtomicInt taskCounter)
    {
        _taskCounter = taskCounter;
        _root = new TimerTaskEntry(null, -1);
        _root.Next = _root;
        _root.Prev = _root;

        _expiration = new AtomicLong(-1);
    }

    public long GetDelay() => Math.Max(GetExpiration() - DateTimeOffset.Now.ToUnixTimeMilliseconds(), 0);

    public int CompareTo(TimerTaskList? other) => GetExpiration().CompareTo(other?.GetExpiration() ?? long.MinValue);

    /// <summary>
    /// Add a timer task entry to this list
    /// </summary>
    public void Add(TimerTaskEntry timerTaskEntry)
    {
        bool done = false;
        while (!done)
        {
            // 将其从其他队列中移除
            // 在lock外操作，避免死锁
            timerTaskEntry.Remove();
            lock (timerTaskEntry)
            {
                if (timerTaskEntry.List == null)
                {
                    //将timerTaskEntry添加到集合的末尾
                    var tail = _root.Prev!;
                    timerTaskEntry.Next = _root;
                    timerTaskEntry.Prev = tail;
                    timerTaskEntry.List = this;
                    tail.Next = timerTaskEntry;
                    _root.Prev = timerTaskEntry;
                    _taskCounter.Increment();
                    done = true;
                }
            }
        }
    }

    /// <summary>
    /// Remove the specified timer task entry from this list
    /// </summary>
    public void Remove(TimerTaskEntry timerTaskEntry)
    {
        lock (this)
        {
            lock (timerTaskEntry)
            {
                if (ReferenceEquals(timerTaskEntry.List, this))
                {
                    timerTaskEntry.Next!.Prev = timerTaskEntry.Prev;
                    timerTaskEntry.Prev!.Next = timerTaskEntry.Next;
                    timerTaskEntry.Next = null;
                    timerTaskEntry.Prev = null;
                    timerTaskEntry.List = null;
                    _taskCounter.Decrement();
                }
            }
        }
    }

    /// <summary>
    /// Remove all task entries and apply the supplied function to each of them
    /// </summary>
    public void Flush(Action<TimerTaskEntry> func)
    {
        lock (this)
        {
            var head = _root.Next;
            while (!ReferenceEquals(head, _root))
            {
                Remove(head!);
                func?.Invoke(head!);
                head = _root.Next;
            }

            _expiration.Set(-1);
        }
    }

    public bool SetExpiration(long expirationMs) => _expiration.Set(expirationMs) != expirationMs;

    public long GetExpiration() => _expiration.Get();
}

internal class TimerTaskEntry
{
    public TimerTaskEntry(TimerTask? timerTask, long expirationMs)
    {
        ExpirationMs = expirationMs;
        // if this timerTask is already held by an existing timer task entry,
        // setTimerTaskEntry will remove it.
        timerTask?.SetTimerTaskEntry(this);
        TimerTask = timerTask;
    }

    public TimerTaskList? List { get; set; }

    public TimerTaskEntry? Next { get; set; }

    public TimerTaskEntry? Prev { get; set; }

    /// <summary>
    /// 过期时间，unix毫秒时间戳
    /// </summary>
    public long ExpirationMs { get; }

    public TimerTask? TimerTask { get; internal set; }

    public bool Cancelled => TimerTask is null || TimerTask.GetTimerTaskEntry() != this;

    public void Remove()
    {
        var currentList = List;
        // If remove is called when another thread is moving the entry from a task entry list to another,
        // this may fail to remove the entry due to the change of value of list. Thus, we retry until the list becomes null.
        // In a rare case, this thread sees null and exits the loop, but the other thread insert the entry to another list later.
        while (currentList != null)
        {
            currentList.Remove(this);
            currentList = List;
        }
    }
}
