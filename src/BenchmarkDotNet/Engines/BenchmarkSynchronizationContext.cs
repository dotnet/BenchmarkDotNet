using JetBrains.Annotations;
using System;
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
    // Use 2 arrays to reduce lock contention while executing. The common case is only 1 callback will be queued at a time.
    private (SendOrPostCallback d, object? state)[] queue = new (SendOrPostCallback d, object? state)[1];
    private (SendOrPostCallback d, object? state)[] executing = new (SendOrPostCallback d, object? state)[1];
    private int queueCount = 0;
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

            int index = queueCount;
            if (++queueCount > queue.Length)
            {
                Array.Resize(ref queue, queue.Length * 2);
            }
            queue[index] = (d, state);

            Monitor.Pulse(queue);
        }
    }

    private void ThrowIfDisposed() => _ = queue ?? throw new ObjectDisposedException(nameof(BenchmarkDotNetSynchronizationContext));

    internal void Dispose()
    {
        int count;
        (SendOrPostCallback d, object? state)[] executing;
        lock (queue)
        {
            ThrowIfDisposed();

            // Flush any remaining posted callbacks.
            count = queueCount;
            queueCount = 0;
            executing = queue;
            queue = null;
        }
        this.executing = null;
        for (int i = 0; i < count; ++i)
        {
            executing[i].d(executing[i].state);
            executing[i] = default;
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
            int count;
            (SendOrPostCallback d, object? state)[] executing;
            lock (queue)
            {
                count = queueCount;
                queueCount = 0;
                executing = queue;
                queue = this.executing;
            }
            this.executing = executing;
            for (int i = 0; i < count; ++i)
            {
                executing[i].d(executing[i].state);
                executing[i] = default;
            }
            if (count > 0)
            {
                // Reset spinner after any posted callback is executed.
                spinner = new();
                continue;
            }

            if (isCompleted)
            {
                return;
            }

            if (!spinner.NextSpinWillYield)
            {
                spinner.SpinOnce();
                continue;
            }

            // Yield the thread and wait for completion or for a posted callback.
            lock (queue)
            {
                Monitor.Wait(queue);
            }
            // Reset the spinner.
            spinner = new();
        }
    }
}
