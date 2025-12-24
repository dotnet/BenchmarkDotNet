using JetBrains.Annotations;
using Perfolizer.Horology;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace BenchmarkDotNet.Engines;

// This is used to prevent allocating a new async state machine on every benchmark iteration.
[UsedImplicitly]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class WorkloadContinuerAndValueTaskSource : ICriticalNotifyCompletion, IValueTaskSource<ClockSpan>
{
    private static readonly Action s_completedSentinel = CompletedMethod;
    private static void CompletedMethod() => throw new InvalidOperationException();

    private Action? continuation;
    private ManualResetValueTaskSourceCore<ClockSpan> valueTaskSourceCore;

    public ValueTask<ClockSpan> Continue()
    {
        valueTaskSourceCore.Reset();
        var callback = continuation;
        continuation = null;
        callback?.Invoke();
        return new(this, valueTaskSourceCore.Version);
    }

    public void Complete()
    {
        var callback = continuation;
        continuation = s_completedSentinel;
        callback?.Invoke();
    }

    public void SetResult(ClockSpan result)
        => valueTaskSourceCore.SetResult(result);

    public void SetException(Exception exception)
        => valueTaskSourceCore.SetException(exception);

    // Await infrastructure
    public WorkloadContinuerAndValueTaskSource GetAwaiter()
        => this;

    public void OnCompleted(Action continuation)
        => UnsafeOnCompleted(continuation);

    public void UnsafeOnCompleted(Action continuation)
        => this.continuation = continuation;

    public bool IsCompleted
        => continuation == s_completedSentinel;

    public void GetResult() { }

    ClockSpan IValueTaskSource<ClockSpan>.GetResult(short token)
        => valueTaskSourceCore.GetResult(token);

    ValueTaskSourceStatus IValueTaskSource<ClockSpan>.GetStatus(short token)
        => valueTaskSourceCore.GetStatus(token);

    void IValueTaskSource<ClockSpan>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        => valueTaskSourceCore.OnCompleted(continuation, state, token, flags);
}