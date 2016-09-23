using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Engines
{
    public class EngineTargetStage : EngineStage
    {
        internal const int MinIterationCount = 15;
        internal const int MaxIterationCount = 100;
        internal const int MaxIdleIterationCount = 20;
        private const double MaxIdleStdErrRelative = 0.05;

        public EngineTargetStage(IEngine engine) : base(engine)
        {
        }

        public List<Measurement> Run(long invokeCount, IterationMode iterationMode, ICharacteristic<int> iterationCount, int unrollFactor)
        {
            return iterationCount.IsDefault
                ? RunAuto(invokeCount, iterationMode, unrollFactor)
                : RunSpecific(invokeCount, iterationMode, iterationCount.SpecifiedValue, unrollFactor);
        }

        public List<Measurement> RunIdle(long invokeCount, int unrollFactor) => Run(invokeCount, IterationMode.IdleTarget, TargetJob.Run.TargetCount.MakeDefault(), unrollFactor);
        public List<Measurement> RunMain(long invokeCount, int unrollFactor) => Run(invokeCount, IterationMode.MainTarget, TargetJob.Run.TargetCount, unrollFactor);

        private List<Measurement> RunAuto(long invokeCount, IterationMode iterationMode, int unrollFactor)
        {
            var measurements = new List<Measurement>();
            int iterationCounter = 0;
            bool isIdle = iterationMode.IsIdle();
            double maxErrorRelative = isIdle ? MaxIdleStdErrRelative : Resolver.Resolve(TargetAccuracy.MaxStdErrRelative);
            while (true)
            {
                iterationCounter++;
                var measurement = RunIteration(iterationMode, iterationCounter, invokeCount, unrollFactor);
                measurements.Add(measurement);

                var statistics = new Statistics(measurements.Select(m => m.Nanoseconds));
                if (Resolver.Resolve(TargetAccuracy.RemoveOutliers))
                    statistics = new Statistics(statistics.WithoutOutliers());
                double actualError = statistics.StandardError;
                double maxError = maxErrorRelative * statistics.Mean;

                if (iterationCounter >= MinIterationCount && actualError < maxError)
                    break;

                if (iterationCounter >= MaxIterationCount || (isIdle && iterationCounter >= MaxIdleIterationCount))
                    break;
            }
            WriteLine();
            return measurements;
        }

        private List<Measurement> RunSpecific(long invokeCount, IterationMode iterationMode, int iterationCount, int unrollFactor)
        {
            var measurements = new List<Measurement>();
            for (int i = 0; i < iterationCount; i++)
                measurements.Add(RunIteration(iterationMode, i + 1, invokeCount, unrollFactor));
            WriteLine();
            return measurements;
        }
    }
}