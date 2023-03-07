using ThinkingHelper.Collections.Generic;

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
namespace ThinkingHelper.Threading.Timers;

/// <summary>
/// 基于kafka分级时间轮的实现
/// https://github.com/apache/kafka/blob/trunk/core/src/main/scala/kafka/utils/timer/TimingWheel.scala
/// </summary>
internal sealed class TimingWheel
{
    private readonly TimerTaskList[] _buckets; //时间槽
    private readonly DelayQueue<TimerTaskList> _delayQueue; //时间槽队列
    private readonly long _interval; //时间轮大小。（固定为_tickMs * _wheelSize）
    private readonly AtomicInt _taskCounter; //任务总数
    private readonly long _tickMs; //时间槽精度
    private readonly int _wheelSize; //时间槽数量
    private long _currentTime; //时间轮 当前时间
    private TimingWheel? _overflowWheel; //溢出时间轮

    /// <summary>
    /// </summary>
    /// <param name="tickMs">每个时间槽的时间</param>
    /// <param name="wheelSize">时间槽数量</param>
    /// <param name="startMs">起始时间。毫秒时间戳</param>
    /// <param name="taskCounter">任务总数</param>
    /// <param name="delayQueue">时间槽队列</param>
    public TimingWheel(long tickMs, int wheelSize, long startMs, AtomicInt taskCounter, DelayQueue<TimerTaskList> delayQueue)
    {
        _tickMs = tickMs;
        _wheelSize = wheelSize;
        _taskCounter = taskCounter;
        _delayQueue = delayQueue;
        _interval = tickMs * wheelSize;
        _currentTime = startMs - startMs % tickMs; //往下取整到tickMs的整数倍
        _buckets = new TimerTaskList[wheelSize];
        for (int i = 0; i < wheelSize; i++)
        {
            _buckets[i] = new TimerTaskList(taskCounter);
        }
    }

    /// <summary>
    /// 获得时间轮中维护的当前时间
    /// </summary>
    public long CurrentTime => _currentTime;

    /// <summary>
    /// 添加任务
    /// </summary>
    /// <returns>任务已取消或者已过期将返回false；成功添加进时间轮中返回true</returns>
    public bool Add(TimerTaskEntry timerTaskEntry)
    {
        long expiration = timerTaskEntry.ExpirationMs;

        if (timerTaskEntry.Cancelled)
        {
            //is Cancelled
            return false;
        }

        if (expiration < _currentTime + _tickMs)
        {
            //Already expired
            return false;
        }

        if (expiration < _currentTime + _interval)
        {
            // Put in its own bucket
            long virtualId = expiration / _tickMs;
            var bucket = _buckets[virtualId % _wheelSize];
            bucket.Add(timerTaskEntry);

            //Set the bucket expiration time
            if (bucket.SetExpiration(virtualId * _tickMs))
            {
                // The bucket needs to be enqueued because it was an expired bucket
                // We only need to enqueue the bucket when its expiration time has changed, i.e. the wheel has advanced
                // and the previous buckets gets reused; further calls to set the expiration within the same wheel cycle
                // will pass in the same value and hence return false, thus the bucket with the same expiration will not
                // be enqueued multiple times.
                _delayQueue.TryEnqueue(bucket);
            }

            return true;
        }

        // Out of the interval. Put it into the parent timer
        if (_overflowWheel is null) AddOverflowWheel();
        return _overflowWheel!.Add(timerTaskEntry);
    }

    /// <summary>
    /// 推进时间
    /// </summary>
    public void AdvanceClock(long timeMs)
    {
        if (timeMs >= _currentTime + _tickMs)
        {
            _currentTime = timeMs - timeMs % _tickMs;

            //尝试推进溢出时间轮
            _overflowWheel?.AdvanceClock(_currentTime);
        }
    }

    /// <summary>
    /// 创建溢出时间轮
    /// </summary>
    private void AddOverflowWheel()
    {
        lock (this)
        {
            if (_overflowWheel is null)
            {
                _overflowWheel = new TimingWheel(
                    _interval,
                    _wheelSize,
                    _currentTime,
                    _taskCounter,
                    _delayQueue
                );
            }
        }
    }
}
