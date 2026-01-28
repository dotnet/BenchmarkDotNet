using Perfolizer.Horology;
using System;
using System.Threading.Tasks;

namespace BenchmarkDotNet.Engines;

internal readonly struct IterationData(IterationMode iterationMode, IterationStage iterationStage, int index, long invokeCount, int unrollFactor,
    Func<ValueTask> setupAction, Func<ValueTask> cleanupAction, Func<long, IClock, ValueTask<ClockSpan>> workloadAction)
{
    public readonly IterationMode mode = iterationMode;
    public readonly IterationStage stage = iterationStage;
    public readonly int index = index;
    public readonly long invokeCount = invokeCount;
    public readonly int unrollFactor = unrollFactor;
    public readonly Func<ValueTask> setupAction = setupAction;
    public readonly Func<ValueTask> cleanupAction = cleanupAction;
    public readonly Func<long, IClock, ValueTask<ClockSpan>> workloadAction = workloadAction;
}