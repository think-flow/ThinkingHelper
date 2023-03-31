using System.Threading;

namespace ThinkingHelper.Threading;

internal static class LongMonitor
{
    internal static bool TryEnter(object obj, long millisecondsTimeout)
    {
        bool lockTaken = false;
        TryEnter(obj, millisecondsTimeout, ref lockTaken);
        return lockTaken;
    }

    //Monitor.TryEnter不支持long长度的timeout毫秒
    //那就将其分多次获取
    internal static void TryEnter(object obj, long millisecondsTimeout, ref bool lockTaken)
    {
        if (millisecondsTimeout <= int.MaxValue)
        {
            Monitor.TryEnter(obj, (int) millisecondsTimeout, ref lockTaken);
            return;
        }

        do
        {
            Monitor.TryEnter(obj, int.MaxValue, ref lockTaken);
            if (lockTaken)
            {
                return;
            }

            millisecondsTimeout -= int.MaxValue;
        } while (millisecondsTimeout > int.MaxValue);

        Monitor.TryEnter(obj, (int) millisecondsTimeout, ref lockTaken);
    }

    internal static bool Wait(object obj, long millisecondsTimeout)
    {
        if (millisecondsTimeout <= int.MaxValue)
        {
            return Monitor.Wait(obj, (int) millisecondsTimeout);
        }

        do
        {
            if (Monitor.Wait(obj, int.MaxValue))
            {
                return true;
            }

            millisecondsTimeout -= int.MaxValue;
        } while (millisecondsTimeout > int.MaxValue);

        return Monitor.Wait(obj, (int) millisecondsTimeout);
    }
}
