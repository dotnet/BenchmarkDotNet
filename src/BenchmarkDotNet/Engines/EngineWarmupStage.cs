using System;
using System.Collections.Generic;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Engines
{
    internal abstract class EngineWarmupStage(IterationMode iterationMode) : EngineStage(IterationStage.Warmup, iterationMode)
    {
        private const int MinOverheadIterationCount = 4;
        internal const int MaxOverheadIterationCount = 10;

        internal static EngineWarmupStage GetOverhead()
            => new EngineWarmupStageAuto(IterationMode.Overhead, MinOverheadIterationCount, MaxOverheadIterationCount);

        internal static EngineWarmupStage GetWorkload(IEngine engine, RunStrategy runStrategy)
        {
            var job = engine.TargetJob;
            var count = job.ResolveValueAsNullable(RunMode.WarmupCountCharacteristic);
            if (count.HasValue && count.Value != EngineResolver.ForceAutoWarmup || runStrategy == RunStrategy.Monitoring)
            {
                return new EngineWarmupStageSpecific(count ?? 0, IterationMode.Workload);
            }

            int minIterationCount = job.ResolveValue(RunMode.MinWarmupIterationCountCharacteristic, engine.Resolver);
            int maxIterationCount = job.ResolveValue(RunMode.MaxWarmupIterationCountCharacteristic, engine.Resolver);
            return new EngineWarmupStageAuto(IterationMode.Overhead, minIterationCount, maxIterationCount);
        }
    }

    internal sealed class EngineWarmupStageAuto(IterationMode iterationMode, int minIterationCount, int maxIterationCount) : EngineWarmupStage(iterationMode)
    {
        private const int MinFluctuationCount = 4;

        private readonly int minIterationCount = minIterationCount;
        private readonly int maxIterationCount = maxIterationCount;

        internal override List<Measurement> GetMeasurementList() => new(maxIterationCount);

        internal override bool GetShouldRunIteration(List<Measurement> measurements, ref long invokeCount)
        {
            int n = measurements.Count;

            if (n >= maxIterationCount)
            {
                return false;
            }
            if (n < minIterationCount)
            {
                return true;
            }

            int direction = -1; // The default "pre-state" is "decrease mode"
            int fluctuationCount = 0;
            for (int i = 1; i < n; i++)
            {
                int nextDirection = Math.Sign(measurements[i].Nanoseconds - measurements[i - 1].Nanoseconds);
                if (nextDirection != direction || nextDirection == 0)
                {
                    direction = nextDirection;
                    fluctuationCount++;
                }
            }

            return fluctuationCount < MinFluctuationCount;
        }
    }

    internal sealed class EngineWarmupStageSpecific(int maxIterationCount, IterationMode iterationMode) : EngineWarmupStage(iterationMode)
    {
        private int iterationCount = 0;

        internal override List<Measurement> GetMeasurementList() => new(maxIterationCount);

        internal override bool GetShouldRunIteration(List<Measurement> measurements, ref long invokeCount)
            => ++iterationCount <= maxIterationCount;
    }
}