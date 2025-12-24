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
public sealed class BenchmarkSynchronizationContext : SynchronizationContext, IDisposable
{
    private readonly SynchronizationContext previousContext;
    private readonly Queue<(SendOrPostCallback d, object? state)> queue = new();

    private BenchmarkSynchronizationContext(SynchronizationContext previousContext)
    {
        this.previousContext = previousContext;
    }

    public override SynchronizationContext CreateCopy()
        => this;

    public override void Post(SendOrPostCallback d, object? state)
        => queue.Enqueue((d ?? throw new ArgumentNullException(nameof(d)), state));

    public static BenchmarkSynchronizationContext CreateAndSetCurrent()
    {
        var context = new BenchmarkSynchronizationContext(Current);
        SetSynchronizationContext(context);
        return context;
    }

    public void Dispose()
        => SetSynchronizationContext(previousContext);

    public void ExecuteUntilComplete(ValueTask valueTask)
    {
        var spinner = new SpinWait();
        while (!valueTask.IsCompleted)
        {
            DoSpin(ref spinner);
        }
        valueTask.GetAwaiter().GetResult();
    }

    public T ExecuteUntilComplete<T>(ValueTask<T> valueTask)
    {
        var spinner = new SpinWait();
        while (!valueTask.IsCompleted)
        {
            DoSpin(ref spinner);
        }
        return valueTask.GetAwaiter().GetResult();
    }

    private void DoSpin(ref SpinWait spinner)
    {
        if (queue.Count <= 0)
        {
            spinner.SpinOnce();
            return;
        }

        do
        {
            var (d, state) = queue.Dequeue();
            d(state);
        }
        while (queue.Count > 0);
        // Reset spinner after any posted callback is executed.
        spinner = new();
    }
}
