using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines;

// Used to ensure async continuations are posted back to the same thread as the benchmark process was started on.
[UsedImplicitly]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class BenchmarkSynchronizationContext : SynchronizationContext
{
    private readonly Queue<(SendOrPostCallback d, object? state)> queue = new();

    private BenchmarkSynchronizationContext() { }

    public override SynchronizationContext CreateCopy()
        => this;

    public override void Post(SendOrPostCallback d, object? state)
        => queue.Enqueue((d ?? throw new ArgumentNullException(nameof(d)), state));

    public static BenchmarkSynchronizationContext CreateAndSetCurrent()
    {
        var context = new BenchmarkSynchronizationContext();
        SetSynchronizationContext(context);
        return context;
    }

    public void ExecuteUntilComplete(ValueTask valueTask)
    {
        var spinner = new SpinWait();
        while (!valueTask.IsCompleted)
        {
            while (queue.Count > 0)
            {
                var (d, state) = queue.Dequeue();
                d(state);
            }
            spinner.SpinOnce();
        }
        valueTask.GetAwaiter().GetResult();
    }
}
