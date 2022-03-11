using System;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Helpers
{
    internal class AwaitHelper
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
            var awaiter = task.GetAwaiter();
            if (!awaiter.IsCompleted)
            {
                lock (awaiterLock)
                {
                    awaiterCompleted = false;
                    awaiter.UnsafeOnCompleted(awaiterCallback);
                    // Check if the callback executed synchronously before blocking.
                    if (awaiterCompleted)
                    {
                        System.Threading.Monitor.Wait(awaiterLock);
                    }
                }
            }
            awaiter.GetResult();
        }

        public T GetResult<T>(ValueTask<T> task)
        {
            var awaiter = task.GetAwaiter();
            if (!awaiter.IsCompleted)
            {
                lock (awaiterLock)
                {
                    awaiterCompleted = false;
                    awaiter.UnsafeOnCompleted(awaiterCallback);
                    // Check if the callback executed synchronously before blocking.
                    if (awaiterCompleted)
                    {
                        System.Threading.Monitor.Wait(awaiterLock);
                    }
                }
            }
            return awaiter.GetResult();
        }
    }
}
