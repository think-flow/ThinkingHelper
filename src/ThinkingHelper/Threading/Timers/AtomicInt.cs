using System.Threading;

namespace ThinkingHelper.Threading.Timers;

/// <summary>
/// 将int类型进行装箱，并提供原子操作
/// </summary>
internal class AtomicInt
{
    private int _value;

    public AtomicInt(int value)
    {
        _value = value;
    }

    public int Get() => _value; //32位整数 读写都是原子性的

    public int Increment() => Interlocked.Increment(ref _value);

    public int Decrement() => Interlocked.Decrement(ref _value);
}
