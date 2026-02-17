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

    public T ExecuteUntilComplete<T>(ValueTask<T> valueTask)
        => context.ExecuteUntilComplete(valueTask);
}

internal sealed class BenchmarkDotNetSynchronizationContext : SynchronizationContext
{
    private readonly SynchronizationContext? previousContext;
    private readonly object syncRoot = new();
    // Use 2 arrays to reduce lock contention while executing. The common case is only 1 callback will be queued at a time.
    private (SendOrPostCallback d, object? state)[]? queue = new (SendOrPostCallback d, object? state)[1];
    private (SendOrPostCallback d, object? state)[]? executing = new (SendOrPostCallback d, object? state)[1];
    private int queueCount = 0;
    private bool isCompleted;

    internal BenchmarkDotNetSynchronizationContext(SynchronizationContext? previousContext)
    {
        this.previousContext = previousContext;
    }

    public override SynchronizationContext CreateCopy()
        => this;

    public override void Post(SendOrPostCallback d, object? state)
    {
        if (d is null) throw new ArgumentNullException(nameof(d));

        lock (syncRoot)
        {
            ThrowIfDisposed();

            int index = queueCount;
            if (++queueCount > queue!.Length)
            {
                Array.Resize(ref queue, queue.Length * 2);
            }
            queue[index] = (d, state);

            Monitor.Pulse(syncRoot);
        }
    }

    private void ThrowIfDisposed() => _ = queue ?? throw new ObjectDisposedException(nameof(BenchmarkDotNetSynchronizationContext));

    internal void Dispose()
    {
        int count;
        (SendOrPostCallback d, object? state)[] executing;
        lock (syncRoot)
        {
            ThrowIfDisposed();

            // Flush any remaining posted callbacks.
            count = queueCount;
            queueCount = 0;
            executing = queue!;
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

    internal T ExecuteUntilComplete<T>(ValueTask<T> valueTask)
    {
        ThrowIfDisposed();

        var awaiter = valueTask.GetAwaiter();
        if (awaiter.IsCompleted)
        {
            return awaiter.GetResult();
        }

        isCompleted = false;
        awaiter.UnsafeOnCompleted(OnCompleted);

        var spinner = new SpinWait();
        while (true)
        {
            int count;
            (SendOrPostCallback d, object? state)[] executing;
            lock (syncRoot)
            {
                if (isCompleted)
                {
                    return awaiter.GetResult();
                }

                count = queueCount;
                queueCount = 0;
                executing = queue!;
                queue = this.executing;

                if (count == 0)
                {
                    if (spinner.NextSpinWillYield)
                    {
                        // Yield the thread and wait for completion or for a posted callback.
                        // Thread-safety note: isCompleted and queueCount must be checked inside the lock body
                        // before calling Monitor.Wait to avoid missing the pulse and waiting forever.
                        Monitor.Wait(syncRoot);
                        goto ResetAndContinue;
                    }
                    else
                    {
                        goto SpinAndContinue;
                    }
                }
            }
            this.executing = executing;
            for (int i = 0; i < count; ++i)
            {
                var (d, state) = executing[i];
                executing[i] = default;
                d(state);
            }

        ResetAndContinue:
            spinner = new();
            continue;

        SpinAndContinue:
            spinner.SpinOnce();
        }
    }

    private void OnCompleted()
    {
        lock (syncRoot)
        {
            isCompleted = true;
            Monitor.Pulse(syncRoot);
        }
    }
}
