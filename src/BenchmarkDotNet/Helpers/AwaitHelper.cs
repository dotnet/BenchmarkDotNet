using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
