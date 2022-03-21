using System;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Helpers
{
    public class AwaitHelper
    {
        private readonly object awaiterLock = new object();
        private readonly Action awaiterCallback;
        private bool awaiterCompleted;

        public AwaitHelper()
        {
            awaiterCallback = AwaiterCallback;
        }

        private void AwaiterCallback()
        {
            lock (awaiterLock)
            {
                awaiterCompleted = true;
                System.Threading.Monitor.Pulse(awaiterLock);
            }
        }

        // we use GetAwaiter().GetResult() because it's fastest way to obtain the result in blocking way,
        // and will eventually throw actual exception, not aggregated one
        public void GetResult(Task task)
        {
            task.GetAwaiter().GetResult();
        }

        public T GetResult<T>(Task<T> task)
        {
            return task.GetAwaiter().GetResult();
        }

        // It is illegal to call GetResult from an uncomplete ValueTask, so we must hook up a callback.
        public void GetResult(ValueTask task)
        {
            // Don't continue on the captured context, as that may result in a deadlock if the user runs this in-process.
            var awaiter = task.ConfigureAwait(false).GetAwaiter();
            if (!awaiter.IsCompleted)
            {
                lock (awaiterLock)
                {
                    awaiterCompleted = false;
                    awaiter.UnsafeOnCompleted(awaiterCallback);
                    // Check if the callback executed synchronously before blocking.
                    if (!awaiterCompleted)
                    {
                        System.Threading.Monitor.Wait(awaiterLock);
                    }
                }
            }
            awaiter.GetResult();
        }

        public T GetResult<T>(ValueTask<T> task)
        {
            // Don't continue on the captured context, as that may result in a deadlock if the user runs this in-process.
            var awaiter = task.ConfigureAwait(false).GetAwaiter();
            if (!awaiter.IsCompleted)
            {
                lock (awaiterLock)
                {
                    awaiterCompleted = false;
                    awaiter.UnsafeOnCompleted(awaiterCallback);
                    // Check if the callback executed synchronously before blocking.
                    if (!awaiterCompleted)
                    {
                        System.Threading.Monitor.Wait(awaiterLock);
                    }
                }
            }
            return awaiter.GetResult();
        }

        public static ValueTask ToValueTaskVoid(Task task)
        {
            return new ValueTask(task);
        }

        public static ValueTask ToValueTaskVoid<T>(Task<T> task)
        {
            return new ValueTask(task);
        }

        public static ValueTask ToValueTaskVoid(ValueTask task)
        {
            return task;
        }

        // ValueTask<T> unfortunately can't be converted to a ValueTask for free, so we must create a state machine.
        // It's not a big deal though, as this is only used for Setup/Cleanup where allocations aren't measured.
        // And in practice, this should never be used, as (Value)Task<T> Setup/Cleanup methods have no utility.
        public static async ValueTask ToValueTaskVoid<T>(ValueTask<T> task)
        {
            _ = await task.ConfigureAwait(false);
        }
    }
}
