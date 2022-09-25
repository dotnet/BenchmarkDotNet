using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Helpers
{
    public class AwaitHelper
    {
        private class ValueTaskWaiter
        {
            private readonly Action awaiterCallback;
            private bool awaiterCompleted;

            internal ValueTaskWaiter()
            {
                awaiterCallback = AwaiterCallback;
            }

            private void AwaiterCallback()
            {
                lock (this)
                {
                    awaiterCompleted = true;
                    System.Threading.Monitor.Pulse(this);
                }
            }

            // Hook up a callback instead of converting to Task to prevent extra allocations on each benchmark run.
            internal void GetResult(ValueTask task)
            {
                // Don't continue on the captured context, as that may result in a deadlock if the user runs this in-process.
                var awaiter = task.ConfigureAwait(false).GetAwaiter();
                if (!awaiter.IsCompleted)
                {
                    lock (this)
                    {
                        awaiterCompleted = false;
                        awaiter.UnsafeOnCompleted(awaiterCallback);
                        // Check if the callback executed synchronously before blocking.
                        if (!awaiterCompleted)
                        {
                            System.Threading.Monitor.Wait(this);
                        }
                    }
                }
                awaiter.GetResult();
            }

            internal T GetResult<T>(ValueTask<T> task)
            {
                // Don't continue on the captured context, as that may result in a deadlock if the user runs this in-process.
                var awaiter = task.ConfigureAwait(false).GetAwaiter();
                if (!awaiter.IsCompleted)
                {
                    lock (this)
                    {
                        awaiterCompleted = false;
                        awaiter.UnsafeOnCompleted(awaiterCallback);
                        // Check if the callback executed synchronously before blocking.
                        if (!awaiterCompleted)
                        {
                            System.Threading.Monitor.Wait(this);
                        }
                    }
                }
                return awaiter.GetResult();
            }
        }

        // We use thread static field so that multiple threads can use individual lock object and callback.
        [ThreadStatic]
        private static ValueTaskWaiter ts_valueTaskWaiter;

        private ValueTaskWaiter CurrentValueTaskWaiter
        {
            get
            {
                if (ts_valueTaskWaiter == null)
                {
                    ts_valueTaskWaiter = new ValueTaskWaiter();
                }
                return ts_valueTaskWaiter;
            }
        }

        // we use GetAwaiter().GetResult() because it's fastest way to obtain the result in blocking way,
        // and will eventually throw actual exception, not aggregated one
        public void GetResult(Task task) => task.GetAwaiter().GetResult();

        public T GetResult<T>(Task<T> task) => task.GetAwaiter().GetResult();

        // ValueTask can be backed by an IValueTaskSource that only supports asynchronous awaits, so we have to hook up a callback instead of calling .GetAwaiter().GetResult() like we do for Task.
        // The alternative is to convert it to Task using .AsTask(), but that causes allocations which we must avoid for memory diagnoser.
        public void GetResult(ValueTask task) => CurrentValueTaskWaiter.GetResult(task);

        public T GetResult<T>(ValueTask<T> task) => CurrentValueTaskWaiter.GetResult(task);

        internal static MethodInfo GetGetResultMethod(Type taskType)
        {
            if (!taskType.IsGenericType)
            {
                return typeof(AwaitHelper).GetMethod(nameof(AwaitHelper.GetResult), BindingFlags.Public | BindingFlags.Instance, null, new Type[1] { taskType }, null);
            }

            Type compareType = taskType.GetGenericTypeDefinition() == typeof(ValueTask<>) ? typeof(ValueTask<>)
                : typeof(Task).IsAssignableFrom(taskType.GetGenericTypeDefinition()) ? typeof(Task<>)
                : null;
            if (compareType == null)
            {
                return null;
            }
            var resultType = taskType
                .GetMethod(nameof(Task.GetAwaiter), BindingFlags.Public | BindingFlags.Instance)
                .ReturnType
                .GetMethod(nameof(TaskAwaiter.GetResult), BindingFlags.Public | BindingFlags.Instance)
                .ReturnType;
            return typeof(AwaitHelper).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(m =>
                {
                    if (m.Name != nameof(AwaitHelper.GetResult)) return false;
                    Type paramType = m.GetParameters().First().ParameterType;
                    // We have to compare the types indirectly, == check doesn't work.
                    return paramType.Assembly == compareType.Assembly && paramType.Namespace == compareType.Namespace && paramType.Name == compareType.Name;
                })
                .MakeGenericMethod(new[] { resultType });
        }
    }
}
