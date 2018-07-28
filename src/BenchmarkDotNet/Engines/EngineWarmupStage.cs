using System;
using System.Collections.Generic;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Engines
{
    internal class EngineWarmupStage : EngineStage
    {
        internal const int MaxOverheadIterationCount = 10;

        private readonly int? warmupCount;
        private readonly int minIterationCount;
        private readonly int maxIterationCount;

        public EngineWarmupStage(IEngine engine) : base(engine)
        {
            warmupCount = engine.TargetJob.ResolveValueAsNullable(RunMode.WarmupCountCharacteristic);
            minIterationCount = engine.TargetJob.ResolveValue(RunMode.MinWarmupIterationCountCharacteristic, engine.Resolver);
            maxIterationCount = engine.TargetJob.ResolveValue(RunMode.MaxWarmupIterationCountCharacteristic, engine.Resolver);
        }

        public void RunOverhead(long invokeCount, int unrollFactor)
            => RunAuto(invokeCount, IterationMode.Overhead, unrollFactor);

        public void RunWorkload(long invokeCount, int unrollFactor, bool forceSpecific = false)
            => Run(invokeCount, IterationMode.Workload, false, unrollFactor, forceSpecific);

        internal List<Measurement> Run(long invokeCount, IterationMode iterationMode, bool runAuto, int unrollFactor, bool forceSpecific = false)
            => (runAuto || warmupCount == null || warmupCount.Value == EngineResolver.ForceAutoWarmup) && !forceSpecific
                ? RunAuto(invokeCount, iterationMode, unrollFactor)
                : RunSpecific(invokeCount, iterationMode, warmupCount ?? 0, unrollFactor);

        private List<Measurement> RunAuto(long invokeCount, IterationMode iterationMode, int unrollFactor)
        {
            var measurements = new List<Measurement>(maxIterationCount);
            int iterationCounter = 0;
            while (true)
            {
                iterationCounter++;
                measurements.Add(RunIteration(iterationMode, IterationStage.Warmup, iterationCounter, invokeCount, unrollFactor));
                if (IsWarmupFinished(measurements, iterationMode))
                    break;
            }
            WriteLine();

            return measurements;
        }

        private List<Measurement> RunSpecific(long invokeCount, IterationMode iterationMode, int iterationCount, int unrollFactor)
        {
            var measurements = new List<Measurement>(maxIterationCount);
            for (int i = 0; i < iterationCount; i++)
                measurements.Add(RunIteration(iterationMode,IterationStage.Warmup, i + 1, invokeCount, unrollFactor));

            WriteLine();

            return measurements;
        }

        private bool IsWarmupFinished(List<Measurement> measurements, IterationMode iterationMode)
        {
            int n = measurements.Count;
            if (n >= maxIterationCount || iterationMode == IterationMode.Overhead && n >= MaxOverheadIterationCount)
                return true;
            if (n < minIterationCount)
                return false;

            int dir = -1, changeCount = 0;
            for (int i = 1; i < n; i++)
            {
                int nextDir = Math.Sign(measurements[i].Nanoseconds - measurements[i - 1].Nanoseconds);
                if (nextDir != dir || nextDir == 0)
                {
                    dir = nextDir;
                    changeCount++;
                }
            }

            return changeCount >= 4;
        }
    }
}