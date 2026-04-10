using JetBrains.Annotations;
using Perfolizer.Horology;
using System.ComponentModel;
using System.Threading.Tasks.Sources;

namespace BenchmarkDotNet.Engines;

// This is used to prevent allocating a new async state machine on every benchmark iteration.
[UsedImplicitly]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class WorkloadValueTaskSource : IValueTaskSource<ClockSpan>, IValueTaskSource<bool>
{
    private ManualResetValueTaskSourceCore<bool> continuerSource;
    private ManualResetValueTaskSourceCore<ClockSpan> clockSpanSource;

    public ValueTask<ClockSpan> Continue()
    {
        clockSpanSource.Reset();
        continuerSource.SetResult(false);
        return new(this, clockSpanSource.Version);
    }

    public ValueTask<bool> GetIsComplete()
        => new(this, continuerSource.Version);

    public void Complete()
        => continuerSource.SetResult(true);

    public ValueTask<bool> SetResultAndGetIsComplete(ClockSpan result)
    {
        continuerSource.Reset();
        clockSpanSource.SetResult(result);
        return GetIsComplete();
    }

    public void SetException(Exception exception)
        => clockSpanSource.SetException(exception);

    ValueTaskSourceStatus IValueTaskSource<bool>.GetStatus(short token)
        => continuerSource.GetStatus(token);

    void IValueTaskSource<bool>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        => continuerSource.OnCompleted(continuation, state, token, flags);

    bool IValueTaskSource<bool>.GetResult(short token)
        => continuerSource.GetResult(token);

    ClockSpan IValueTaskSource<ClockSpan>.GetResult(short token)
        => clockSpanSource.GetResult(token);

    ValueTaskSourceStatus IValueTaskSource<ClockSpan>.GetStatus(short token)
        => clockSpanSource.GetStatus(token);

    void IValueTaskSource<ClockSpan>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        => clockSpanSource.OnCompleted(continuation, state, token, flags);
}