using System;
using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Engines
{
    internal class EngineWarmupStage : EngineStage
    {
        internal const int MinIterationCount = 6;
        internal const int MaxIterationCount = 50;
        internal const int MaxIdleItertaionCount = 10;

        private readonly ICharacteristic<int> warmupCount;
        private readonly List<Measurement> measurements;

        public EngineWarmupStage(IEngine engine) : base(engine)
        {
            warmupCount = engine.TargetJob.Run.WarmupCount;
            var maxSize = ShouldRunAuto(warmupCount) ? MaxIterationCount : warmupCount.SpecifiedValue;
            measurements = new List<Measurement>(maxSize);
        }

        public void RunIdle(long invokeCount, int unrollFactor) 
            => Run(invokeCount, IterationMode.IdleWarmup, warmupCount.MakeDefault(), unrollFactor);

        public void RunMain(long invokeCount, int unrollFactor) 
            => Run(invokeCount, IterationMode.MainWarmup, warmupCount, unrollFactor);

        internal List<Measurement> Run(long invokeCount, IterationMode iterationMode, ICharacteristic<int> iterationCount, int unrollFactor)
        {
            return ShouldRunAuto(iterationCount)
                ? RunAuto(invokeCount, iterationMode, unrollFactor)
                : RunSpecific(invokeCount, iterationMode, iterationCount.SpecifiedValue, unrollFactor);
        }

        private List<Measurement> RunAuto(long invokeCount, IterationMode iterationMode, int unrollFactor)
        {
            int iterationCounter = 0;
            while (true)
            {
                iterationCounter++;
                measurements.Add(RunIteration(iterationMode, iterationCounter, invokeCount, unrollFactor));
                if (IsWarmupFinished(measurements, iterationMode))
                    break;
            }
            if (!IsDiagnoserAttached) WriteLine();

            return measurements;
        }

        private List<Measurement> RunSpecific(long invokeCount, IterationMode iterationMode, int iterationCount, int unrollFactor)
        {
            for (int i = 0; i < iterationCount; i++)
                measurements.Add(RunIteration(iterationMode, i + 1, invokeCount, unrollFactor));

            if (!IsDiagnoserAttached) WriteLine();

            return measurements;
        }

        private static bool IsWarmupFinished(List<Measurement> measurements, IterationMode iterationMode)
        {
            int n = measurements.Count;
            if (n >= MaxIterationCount || (iterationMode.IsIdle() && n >= MaxIdleItertaionCount))
                return true;
            if (n < MinIterationCount)
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