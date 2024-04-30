using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Helpers
{
    public static class AwaitHelper
    {
        private class ValueTaskWaiter
        {
            // We use thread static field so that each thread uses its own individual callback and reset event.
            [ThreadStatic]
            private static ValueTaskWaiter ts_current;
            internal static ValueTaskWaiter Current => ts_current ??= new ValueTaskWaiter();

            // We cache the callback to prevent allocations for memory diagnoser.
            private readonly Action awaiterCallback;
            private readonly ManualResetEventSlim resetEvent;

            private ValueTaskWaiter()
            {
                resetEvent = new ();
                awaiterCallback = resetEvent.Set;
            }

            internal void Wait<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion
            {
                resetEvent.Reset();
                awaiter.UnsafeOnCompleted(awaiterCallback);

                // The fastest way to wait for completion is to spin a bit before waiting on the event. This is the same logic that Task.GetAwaiter().GetResult() uses.
                var spinner = new SpinWait();
                while (!resetEvent.IsSet)
                {
                    if (spinner.NextSpinWillYield)
                    {
                        resetEvent.Wait();
                        return;
                    }
                    spinner.SpinOnce();
                }
            }
        }

        // we use GetAwaiter().GetResult() because it's fastest way to obtain the result in blocking way,
        // and will eventually throw actual exception, not aggregated one
        public static void GetResult(Task task) => task.GetAwaiter().GetResult();

        public static T GetResult<T>(Task<T> task) => task.GetAwaiter().GetResult();

        // ValueTask can be backed by an IValueTaskSource that only supports asynchronous awaits,
        // so we have to hook up a callback instead of calling .GetAwaiter().GetResult() like we do for Task.
        // The alternative is to convert it to Task using .AsTask(), but that causes allocations which we must avoid for memory diagnoser.
        public static void GetResult(ValueTask task)
        {
            // Don't continue on the captured context, as that may result in a deadlock if the user runs this in-process.
            var awaiter = task.ConfigureAwait(false).GetAwaiter();
            if (!awaiter.IsCompleted)
            {
                ValueTaskWaiter.Current.Wait(awaiter);
            }
            awaiter.GetResult();
        }

        public static T GetResult<T>(ValueTask<T> task)
        {
            // Don't continue on the captured context, as that may result in a deadlock if the user runs this in-process.
            var awaiter = task.ConfigureAwait(false).GetAwaiter();
            if (!awaiter.IsCompleted)
            {
                ValueTaskWaiter.Current.Wait(awaiter);
            }
            return awaiter.GetResult();
        }

        internal static MethodInfo GetGetResultMethod(Type taskType)
        {
            if (!taskType.IsGenericType)
            {
                return typeof(AwaitHelper).GetMethod(nameof(AwaitHelper.GetResult), BindingFlags.Public | BindingFlags.Static, null, new Type[1] { taskType }, null);
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
            return typeof(AwaitHelper).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m =>
                {
                    if (m.Name != nameof(AwaitHelper.GetResult)) return false;
                    Type paramType = m.GetParameters().First().ParameterType;
                    return paramType.IsGenericType && paramType.GetGenericTypeDefinition() == compareType;
                })
                .MakeGenericMethod(new[] { resultType });
        }
    }
}
