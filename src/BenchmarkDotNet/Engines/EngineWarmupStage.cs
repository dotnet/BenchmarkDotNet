using System;
using System.Collections.Generic;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Engines
{
    internal abstract class EngineWarmupStage(IterationMode iterationMode, long invokeCount, int unrollFactor, EngineParameters parameters) : EngineStage(IterationStage.Warmup, iterationMode, parameters)
    {
        private const int MinOverheadIterationCount = 4;
        internal const int MaxOverheadIterationCount = 10;

        internal static EngineWarmupStage GetOverhead(long invokeCount, int unrollFactor, EngineParameters parameters)
            => new EngineWarmupStageAuto(IterationMode.Overhead, MinOverheadIterationCount, MaxOverheadIterationCount, invokeCount, unrollFactor, parameters);

        internal static EngineWarmupStage GetWorkload(RunStrategy runStrategy, long invokeCount, int unrollFactor, EngineParameters parameters)
        {
            var job = parameters.TargetJob;
            var count = job.ResolveValueAsNullable(RunMode.WarmupCountCharacteristic);
            if (count.HasValue && count.Value != EngineResolver.ForceAutoWarmup || runStrategy == RunStrategy.Monitoring)
            {
                return new EngineWarmupStageSpecific(count ?? 0, IterationMode.Workload, invokeCount, unrollFactor, parameters);
            }

            int minIterationCount = job.ResolveValue(RunMode.MinWarmupIterationCountCharacteristic, parameters.Resolver);
            int maxIterationCount = job.ResolveValue(RunMode.MaxWarmupIterationCountCharacteristic, parameters.Resolver);
            return new EngineWarmupStageAuto(IterationMode.Workload, minIterationCount, maxIterationCount, invokeCount, unrollFactor, parameters);
        }

        protected IterationData GetIterationData()
            => new(Mode, Stage, ++iterationIndex, invokeCount, unrollFactor, parameters.IterationSetupAction, parameters.IterationCleanupAction,
                Mode == IterationMode.Workload
                ? unrollFactor == 1 ? parameters.WorkloadActionNoUnroll : parameters.WorkloadActionUnroll
                : unrollFactor == 1 ? parameters.OverheadActionNoUnroll: parameters.OverheadActionUnroll);
    }

    internal sealed class EngineWarmupStageAuto(IterationMode iterationMode, int minIterationCount, int maxIterationCount, long invokeCount, int unrollFactor, EngineParameters parameters)
        : EngineWarmupStage(iterationMode, invokeCount, unrollFactor, parameters)
    {
        private const int MinFluctuationCount = 4;

        private readonly int minIterationCount = minIterationCount;
        private readonly int maxIterationCount = maxIterationCount;

        internal override List<Measurement> GetMeasurementList() => new(maxIterationCount);

        internal override bool GetShouldRunIteration(List<Measurement> measurements, out IterationData iterationData)
        {
            int n = measurements.Count;

            if (n >= maxIterationCount)
            {
                iterationData = default;
                return false;
            }
            if (n < minIterationCount)
            {
                iterationData = GetIterationData();
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

            iterationData = GetIterationData();
            return fluctuationCount < MinFluctuationCount;
        }
    }

    internal sealed class EngineWarmupStageSpecific(int maxIterationCount, IterationMode iterationMode, long invokeCount, int unrollFactor, EngineParameters parameters)
        : EngineWarmupStage(iterationMode, invokeCount, unrollFactor, parameters)
    {
        internal override List<Measurement> GetMeasurementList() => new(maxIterationCount);

        internal override bool GetShouldRunIteration(List<Measurement> measurements, out IterationData iterationData)
        {
            if (iterationIndex < maxIterationCount)
            {
                iterationData = GetIterationData();
                return true;
            }

            iterationData = default;
            return false;
        }
    }
}