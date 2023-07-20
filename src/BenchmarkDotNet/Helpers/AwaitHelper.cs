using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Helpers
{
    public static class AwaitHelper
    {
        private class ValueTaskWaiter
        {
            // We use thread static field so that multiple threads can use individual lock object and callback.
            [ThreadStatic]
            private static ValueTaskWaiter ts_current;
            internal static ValueTaskWaiter Current => ts_current ??= new ValueTaskWaiter();

            private readonly Action awaiterCallback;
            private bool awaiterCompleted;

            private ValueTaskWaiter()
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
            internal void Wait(ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter)
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

            internal void Wait<T>(ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter awaiter)
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
        }

        // we use GetAwaiter().GetResult() because it's fastest way to obtain the result in blocking way,
        // and will eventually throw actual exception, not aggregated one
        public static void GetResult(Task task) => task.GetAwaiter().GetResult();

        public static T GetResult<T>(Task<T> task) => task.GetAwaiter().GetResult();

        // ValueTask can be backed by an IValueTaskSource that only supports asynchronous awaits, so we have to hook up a callback instead of calling .GetAwaiter().GetResult() like we do for Task.
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

        internal static MethodInfo GetGetResultMethod(Type taskType) => GetMethod(taskType, nameof(AwaitHelper.GetResult));

        internal static MethodInfo GetToValueTaskMethod(Type taskType) => GetMethod(taskType, nameof(AwaitHelper.ToValueTaskVoid));

        private static MethodInfo GetMethod(Type taskType, string methodName)
        {
            if (!taskType.IsGenericType)
            {
                return typeof(AwaitHelper).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, new Type[1] { taskType }, null);
            }

            Type compareType = taskType.GetGenericTypeDefinition() == typeof(ValueTask<>) ? typeof(ValueTask<>)
                : typeof(Task).IsAssignableFrom(taskType) ? typeof(Task<>)
                : null;
            return compareType == null
                ? null
                : typeof(AwaitHelper).GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(m =>
                    {
                        if (m.Name != methodName) return false;
                        Type paramType = m.GetParameters().First().ParameterType;
                        // We have to compare the types indirectly, == check doesn't work.
                        return paramType.Assembly == compareType.Assembly && paramType.Namespace == compareType.Namespace && paramType.Name == compareType.Name;
                    })
                    .MakeGenericMethod(new[]
                    {
                        taskType
                        .GetMethod(nameof(Task.GetAwaiter), BindingFlags.Public | BindingFlags.Instance).ReturnType
                        .GetMethod(nameof(TaskAwaiter.GetResult), BindingFlags.Public | BindingFlags.Instance).ReturnType
                    });
        }
    }
}