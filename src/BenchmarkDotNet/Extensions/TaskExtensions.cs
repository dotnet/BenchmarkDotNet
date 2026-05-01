#if NETSTANDARD2_0
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Extensions;

internal static class TaskExtensions
{
    public static Task WaitAsync(this Task task, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }
        if (task.IsCompleted)
        {
            return task;
        }
        return Core();

        async Task Core()
        {
            var timeoutTaskSource = new TaskCompletionSource<object>();
            using var _ = cancellationToken.Register(() => timeoutTaskSource.TrySetCanceled(cancellationToken), false);
            await Task.WhenAny(task, timeoutTaskSource.Task).Unwrap().ConfigureAwait(false);
        }
    }

    public static Task WaitAsync(this Task task, TimeSpan timeout)
    {
        if (task.IsCompleted)
        {
            return task;
        }
        return Core();

        async Task Core()
        {
            var timeoutTaskSource = new TaskCompletionSource<object>();
            using var cts = new CancellationTokenSource(timeout);
            using var _ = cts.Token.Register(() => timeoutTaskSource.SetException(new TimeoutException()), false);
            await Task.WhenAny(task, timeoutTaskSource.Task).Unwrap().ConfigureAwait(false);
        }
    }

    public static Task WaitAsync(this Task task, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }
        if (task.IsCompleted)
        {
            return task;
        }
        return Core();

        async Task Core()
        {
            var timeoutTaskSource = new TaskCompletionSource<object>();
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var _ = cancellationToken.Register(() => timeoutTaskSource.TrySetCanceled(cancellationToken), false);
            using var __ = timeoutCts.Token.Register(() => timeoutTaskSource.TrySetException(new TimeoutException()), false);
            await Task.WhenAny(task, timeoutTaskSource.Task).Unwrap().ConfigureAwait(false);
        }
    }

    public static Task<T> WaitAsync<T>(this Task<T> task, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<T>(cancellationToken);
        }
        if (task.IsCompleted)
        {
            return task;
        }
        return Core();

        async Task<T> Core()
        {
            var timeoutTaskSource = new TaskCompletionSource<T>();
            using var _ = cancellationToken.Register(() => timeoutTaskSource.TrySetCanceled(cancellationToken), false);
            return await Task.WhenAny(task, timeoutTaskSource.Task).Unwrap().ConfigureAwait(false);
        }
    }

    public static Task<T> WaitAsync<T>(this Task<T> task, TimeSpan timeout)
    {
        if (task.IsCompleted)
        {
            return task;
        }
        return Core();

        async Task<T> Core()
        {
            var timeoutTaskSource = new TaskCompletionSource<T>();
            using var cts = new CancellationTokenSource(timeout);
            using var _ = cts.Token.Register(() => timeoutTaskSource.SetException(new TimeoutException()), false);
            return await Task.WhenAny(task, timeoutTaskSource.Task).Unwrap().ConfigureAwait(false);
        }
    }

    public static Task<T> WaitAsync<T>(this Task<T> task, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<T>(cancellationToken);
        }
        if (task.IsCompleted)
        {
            return task;
        }
        return Core();

        async Task<T> Core()
        {
            var timeoutTaskSource = new TaskCompletionSource<T>();
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var _ = cancellationToken.Register(() => timeoutTaskSource.TrySetCanceled(cancellationToken), false);
            using var __ = timeoutCts.Token.Register(() => timeoutTaskSource.TrySetException(new TimeoutException()), false);
            return await Task.WhenAny(task, timeoutTaskSource.Task).Unwrap().ConfigureAwait(false);
        }
    }
}
#endif
