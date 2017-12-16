using System;
using System.Collections.Generic;
using BenchmarkDotNet.Horology;
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
        internal const double MaxIdleRelativeError = 0.05;
        internal const int DefaultTargetCount = 10;

        private readonly int? targetCount;
        private readonly double maxRelativeError;
        private readonly TimeInterval? maxAbsoluteError;
        private readonly bool removeOutliers;

        public EngineTargetStage(IEngine engine) : base(engine)
        {
            targetCount = engine.TargetJob.ResolveValueAsNullable(RunMode.TargetCountCharacteristic);
            maxRelativeError = engine.TargetJob.ResolveValue(AccuracyMode.MaxRelativeErrorCharacteristic, engine.Resolver);
            maxAbsoluteError = engine.TargetJob.ResolveValueAsNullable(AccuracyMode.MaxAbsoluteErrorCharacteristic);
            removeOutliers = engine.TargetJob.ResolveValue(AccuracyMode.RemoveOutliersCharacteristic, engine.Resolver);
        }

        public IReadOnlyList<Measurement> RunIdle(long invokeCount, int unrollFactor) 
            => RunAuto(invokeCount, IterationMode.IdleTarget, unrollFactor);

        public IReadOnlyList<Measurement> RunMain(long invokeCount, int unrollFactor, bool forceSpecific = false) 
            => Run(invokeCount, IterationMode.MainTarget, false, unrollFactor, forceSpecific);

        internal IReadOnlyList<Measurement> Run(long invokeCount, IterationMode iterationMode, bool runAuto, int unrollFactor, bool forceSpecific = false)
            => (runAuto || targetCount == null) && !forceSpecific
                ? RunAuto(invokeCount, iterationMode, unrollFactor)
                : RunSpecific(invokeCount, iterationMode, (targetCount ?? DefaultTargetCount), unrollFactor);

        private List<Measurement> RunAuto(long invokeCount, IterationMode iterationMode, int unrollFactor)
        {
            var measurements = new List<Measurement>(MaxIterationCount);
            var measurementsForStatistics = new List<Measurement>(MaxIterationCount);

            int iterationCounter = 0;
            bool isIdle = iterationMode.IsIdle();
            double effectiveMaxRelativeError = isIdle ? MaxIdleRelativeError : maxRelativeError;
            while (true)
            {
                iterationCounter++;
                var measurement = RunIteration(iterationMode, iterationCounter, invokeCount, unrollFactor);
                measurements.Add(measurement);
                measurementsForStatistics.Add(measurement);

                var statistics = MeasurementsStatistics.Calculate(measurementsForStatistics, removeOutliers);
                double actualError = statistics.ConfidenceInterval.Margin;

                double maxError1 = effectiveMaxRelativeError * statistics.Mean;
                double maxError2 = maxAbsoluteError?.Nanoseconds ?? double.MaxValue;
                double maxError = Math.Min(maxError1, maxError2);

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
            var measurements = new List<Measurement>(MaxIterationCount);

            for (int i = 0; i < iterationCount; i++)
                measurements.Add(RunIteration(iterationMode, i + 1, invokeCount, unrollFactor));

            WriteLine();

            return measurements;
        }
    }
}