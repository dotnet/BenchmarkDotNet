using System.Runtime.CompilerServices;

namespace BenchmarkDotNet.Helpers;

internal static class ThreadPoolHelper
{
    internal static SwitchToThreadPoolAwaiter Switch(CancellationToken cancellationToken)
        => new(cancellationToken);

    internal readonly struct SwitchToThreadPoolAwaiter(CancellationToken cancellationToken) : ICriticalNotifyCompletion
    {
        public bool IsCompleted
            => cancellationToken.IsCancellationRequested || Thread.CurrentThread.IsThreadPoolThread;

        public SwitchToThreadPoolAwaiter GetAwaiter()
            => this;

        public void GetResult()
            => cancellationToken.ThrowIfCancellationRequested();

        public void OnCompleted(Action continuation)
            => ThreadPool.QueueUserWorkItem(static state => ((Action) state!)(), continuation);

        public void UnsafeOnCompleted(Action continuation)
            => ThreadPool.UnsafeQueueUserWorkItem(static state => ((Action) state!)(), continuation);
    }
}
