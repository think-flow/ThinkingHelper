using System.Threading;

namespace ThinkingHelper.Threading.Timers;

/// <summary>
/// 将int类型进行装箱，并提供原子操作
/// </summary>
internal class AtomicLong
{
    private long _value;

    public AtomicLong(long value)
    {
        _value = value;
    }

    public long Get() => Interlocked.Read(ref _value);

    public long Set(long value) => Interlocked.Exchange(ref _value, value);
}
