using System;

namespace BenchmarkDotNet.Engines
{
    internal readonly struct IterationData(IterationMode iterationMode, IterationStage iterationStage, int index, long invokeCount, int unrollFactor,
        Action setupAction, Action cleanupAction, Action<long> workloadAction)
    {
        public readonly IterationMode mode = iterationMode;
        public readonly IterationStage stage = iterationStage;
        public readonly int index = index;
        public readonly long invokeCount = invokeCount;
        public readonly int unrollFactor = unrollFactor;
        public readonly Action setupAction = setupAction;
        public readonly Action cleanupAction = cleanupAction;
        public readonly Action<long> workloadAction = workloadAction;
    }
}