using System;
using System.Threading.Tasks;
using HideFromIntelliSense = System.ComponentModel.EditorBrowsableAttribute; // we don't want people to use it

namespace BenchmarkDotNet.Running
{
    // if you want to rename any of these methods you need to update DeclarationsProvider's code as well
    // ReSharper disable MemberCanBePrivate.Global
    public static class TaskMethodInvoker
    {
        // can't use Task.CompletedTask here because it's new in .NET 4.6 (we target 4.5)
        private static readonly Task Completed = Task.FromResult((object)null);

        [HideFromIntelliSense(System.ComponentModel.EditorBrowsableState.Never)]
        public static void Idle() => ExecuteBlocking(() => Completed);

        // we use GetAwaiter().GetResult() because it's fastest way to obtain the result in blocking way, 
        // and will eventually throw actual exception, not aggregated one
        [HideFromIntelliSense(System.ComponentModel.EditorBrowsableState.Never)]
        public static void ExecuteBlocking(Func<Task> future) => future.Invoke().GetAwaiter().GetResult();
    }

    public static class TaskMethodInvoker<T>
    {
        private static readonly Task<T> Completed = Task.FromResult(default(T));

        [HideFromIntelliSense(System.ComponentModel.EditorBrowsableState.Never)]
        public static T Idle() => ExecuteBlocking(() => Completed);

        // we use GetAwaiter().GetResult() because it's fastest way to obtain the result in blocking way, 
        // and will eventually throw actual exception, not aggregated one
        [HideFromIntelliSense(System.ComponentModel.EditorBrowsableState.Never)]
        public static T ExecuteBlocking(Func<Task<T>> future) => future.Invoke().GetAwaiter().GetResult();
    }

    public static class ValueTaskMethodInvoker<T>
    {
        [HideFromIntelliSense(System.ComponentModel.EditorBrowsableState.Never)]
        public static T Idle() => ExecuteBlocking(() => new ValueTask<T>(default(T)));

        // we use .Result instead of .GetAwaiter().GetResult() because it's faster
        [HideFromIntelliSense(System.ComponentModel.EditorBrowsableState.Never)]
        public static T ExecuteBlocking(Func<ValueTask<T>> future) => future.Invoke().Result;
    }
    // ReSharper restore MemberCanBePrivate.Global
}