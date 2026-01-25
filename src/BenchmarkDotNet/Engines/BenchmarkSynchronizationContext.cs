using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines;

// Used to ensure async continuations are posted back to the same thread that the benchmark process was started on.
[UsedImplicitly]
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly ref struct BenchmarkSynchronizationContext : IDisposable
{
    private readonly BenchmarkDotNetSynchronizationContext context;

    private BenchmarkSynchronizationContext(BenchmarkDotNetSynchronizationContext context)
    {
        this.context = context;
    }

    public static BenchmarkSynchronizationContext CreateAndSetCurrent()
    {
        var context = new BenchmarkDotNetSynchronizationContext(SynchronizationContext.Current);
        SynchronizationContext.SetSynchronizationContext(context);
        return new(context);
    }

    public void Dispose()
        => context.Dispose();

    public void ExecuteUntilComplete(ValueTask valueTask)
        => context.ExecuteUntilComplete(valueTask);

    public T ExecuteUntilComplete<T>(ValueTask<T> valueTask)
        => context.ExecuteUntilComplete(valueTask);
}

internal sealed class BenchmarkDotNetSynchronizationContext : SynchronizationContext
{
    private readonly SynchronizationContext? previousContext;
    private readonly Queue<(SendOrPostCallback d, object? state)> queue = new();
    private bool isDisposed;
    volatile private bool isCompleted;

    internal BenchmarkDotNetSynchronizationContext(SynchronizationContext? previousContext)
    {
        this.previousContext = previousContext;
    }

    public override SynchronizationContext CreateCopy()
        => this;

    public override void Post(SendOrPostCallback d, object? state)
    {
        if (d is null) throw new ArgumentNullException(nameof(d));

        lock (queue)
        {
            ThrowIfDisposed();

            queue.Enqueue((d, state));
            Monitor.Pulse(queue);
        }
    }

    private void ThrowIfDisposed()
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(BenchmarkDotNetSynchronizationContext));
    }

    internal void Dispose()
    {
        lock (queue)
        {
            ThrowIfDisposed();
            isDisposed = true;

            // Flush any remaining posted callbacks.
            while (TryDequeue(out var callbackAndState))
            {
                callbackAndState.d(callbackAndState.state);
            }
        }
        SetSynchronizationContext(previousContext);
    }

    internal void ExecuteUntilComplete(ValueTask valueTask)
    {
        ThrowIfDisposed();

        var awaiter = valueTask.GetAwaiter();
        if (valueTask.IsCompleted)
        {
            awaiter.GetResult();
            return;
        }

        isCompleted = false;
        awaiter.UnsafeOnCompleted(OnCompleted);
        ExecuteUntilComplete();
        awaiter.GetResult();
    }

    internal T ExecuteUntilComplete<T>(ValueTask<T> valueTask)
    {
        ThrowIfDisposed();

        var awaiter = valueTask.GetAwaiter();
        if (valueTask.IsCompleted)
        {
            return awaiter.GetResult();
        }

        isCompleted = false;
        awaiter.UnsafeOnCompleted(OnCompleted);
        ExecuteUntilComplete();
        return awaiter.GetResult();
    }

    private void OnCompleted()
    {
        isCompleted = true;
        lock (queue)
        {
            Monitor.Pulse(queue);
        }
    }

    private void ExecuteUntilComplete()
    {
        var spinner = new SpinWait();
        while (true)
        {
            if (TryDequeue(out var callbackAndState))
            {
                do
                {
                    callbackAndState.d(callbackAndState.state);
                }
                while (TryDequeue(out callbackAndState));
                // Reset spinner after any posted callback is executed.
                spinner = new();
            }

            if (isCompleted)
            {
                return;
            }

            if (spinner.NextSpinWillYield)
            {
                // Yield the thread and wait for completion or for a posted callback.
                lock (queue)
                {
                    Monitor.Wait(queue);
                }
                // Reset the spinner.
                spinner = new();
                continue;
            }

            spinner.SpinOnce();
        }
    }

    private bool TryDequeue(out (SendOrPostCallback d, object? state) callbackAndState)
    {
        lock (queue)
        {
#if NETSTANDARD2_0
            if (queue.Count > 0)
            {
                callbackAndState = queue.Dequeue();
                return true;
            }
            callbackAndState = default;
            return false;
#else
            return queue.TryDequeue(out callbackAndState);
#endif
        }
    }
}
