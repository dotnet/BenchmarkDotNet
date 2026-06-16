using BenchmarkDotNet.Attributes.CompilerServices;
using JetBrains.Annotations;
using System.ComponentModel;

namespace BenchmarkDotNet.Engines;

// Used to ensure async continuations are posted back to the thread that started the benchmarks.
[UsedImplicitly]
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly ref struct BenchmarkSynchronizationContext : IDisposable
{
    // If this is non-null, we post task continuations to it, otherwise we use task.ConfigureAwait(true) (see AwaitHelper).
    [ThreadStatic]
    internal static SingleThreadPumpContext? Current;

    private readonly SingleThreadPumpContext context;

    private BenchmarkSynchronizationContext(SingleThreadPumpContext context)
    {
        this.context = context;
    }

    public static BenchmarkSynchronizationContext CreateAndSetCurrent()
    {
        if (Current is not null)
        {
            throw new InvalidOperationException($"{nameof(BenchmarkSynchronizationContext)} is already in use.");
        }
        var context = new SingleThreadPumpContext();
        Current = context;
        return new(context);
    }

    public void Dispose()
        => context.Dispose();

    public T ExecuteUntilComplete<T>(ValueTask<T> valueTask)
        => context.ExecuteUntilComplete(valueTask);
}

// We implement a specialized context that does not inherit from SynchronizationContext, because we never install a SynchronizationContext.Current.
[AggressivelyOptimizeMethods]
internal sealed class SingleThreadPumpContext
{
    // Pooled so we don't allocate per await. Carries the continuation to run, and doubles as an intrusive node
    // for the free list (touched only on the pump thread) and the ready list (written by completing threads).
    private sealed class Waiter
    {
        public readonly Action OnCompleted;
        public Action? Continuation;
        public Waiter? Next;
        private readonly SingleThreadPumpContext owner;

        public Waiter(SingleThreadPumpContext owner)
        {
            this.owner = owner;
            OnCompleted = Complete;
        }

        private void Complete() => owner.MarkReady(this);
    }

    private readonly Thread callerThread;
    private readonly object syncRoot = new();
    private Waiter? freeWaiters;
    private Waiter? readyWaiters;
    private bool isCompleted;
    private bool disposed;
    private int outstanding;

    internal SingleThreadPumpContext()
    {
        callerThread = Thread.CurrentThread;
    }

    private void EnsureValid()
    {
        if (disposed)
            throw new ObjectDisposedException(nameof(SingleThreadPumpContext));
        if (callerThread != Thread.CurrentThread)
            throw new InvalidOperationException($"{nameof(SingleThreadPumpContext)} can only be used from the thread it was created on.");
    }

    internal Action GetPassthroughContinuation(Action continuation)
    {
        ArgumentNullException.ThrowIfNull(continuation);
        EnsureValid();
        var waiter = freeWaiters;
        if (waiter is null)
        {
            waiter = new Waiter(this);
        }
        else
        {
            freeWaiters = waiter.Next;
        }
        waiter.Continuation = continuation;
        waiter.Next = null;
        outstanding++;
        return waiter.OnCompleted;
    }

    internal void Post(Action continuation) => GetPassthroughContinuation(continuation).Invoke();

    private void MarkReady(Waiter waiter)
    {
        lock (syncRoot)
        {
            waiter.Next = readyWaiters;
            readyWaiters = waiter;
            Monitor.Pulse(syncRoot);
        }
    }

    internal void Dispose()
    {
        EnsureValid();
        if (outstanding != 0)
        {
            throw new InvalidOperationException($"{nameof(SingleThreadPumpContext)} disposed while there are still pending continuations.");
        }
        disposed = true;
        BenchmarkSynchronizationContext.Current = null;
    }

    internal T ExecuteUntilComplete<T>(ValueTask<T> valueTask)
    {
        EnsureValid();

        var awaiter = valueTask.GetAwaiter();
        if (awaiter.IsCompleted)
        {
            return awaiter.GetResult();
        }

        isCompleted = false;
        awaiter.UnsafeOnCompleted(OnCompleted);

        while (true)
        {
            Waiter waiter;
            lock (syncRoot)
            {
                while (readyWaiters is null && !isCompleted)
                {
                    Monitor.Wait(syncRoot);
                }
                if (readyWaiters is null)
                {
                    return awaiter.GetResult();
                }
                waiter = readyWaiters;
                readyWaiters = waiter.Next;
            }

            var continuation = waiter.Continuation!;
            waiter.Continuation = null;
            waiter.Next = freeWaiters; // return to the pool before invoking, so a re-suspend can reuse it
            freeWaiters = waiter;
            outstanding--;
            continuation.Invoke();
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
