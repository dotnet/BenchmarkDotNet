using System;
using System.Collections.Generic;
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
        internal const double MaxIdleStdErrRelative = 0.05;

        private readonly int? targetCount;
        private readonly double maxStdErrRelative;
        private readonly bool removeOutliers;
        private readonly MeasurementsPool measurementsPool;


        public EngineTargetStage(IEngine engine) : base(engine)
        {
            targetCount = engine.TargetJob.ResolveValueAsNullable(RunMode.TargetCountCharacteristic);
            maxStdErrRelative = engine.TargetJob.ResolveValue(AccuracyMode.MaxStdErrRelativeCharacteristic, engine.Resolver);
            removeOutliers = engine.TargetJob.ResolveValue(AccuracyMode.RemoveOutliersCharacteristic, engine.Resolver);
            measurementsPool = MeasurementsPool.PreAllocate(10, MaxIterationCount, targetCount);
        }

        public IReadOnlyList<Measurement> RunIdle(long invokeCount, int unrollFactor) 
            => RunAuto(invokeCount, IterationMode.IdleTarget, unrollFactor);

        public IReadOnlyList<Measurement> RunMain(long invokeCount, int unrollFactor) 
            => Run(invokeCount, IterationMode.MainTarget, false, unrollFactor);

        internal IReadOnlyList<Measurement> Run(long invokeCount, IterationMode iterationMode, bool runAuto, int unrollFactor)
            => runAuto || targetCount == null
                ? RunAuto(invokeCount, iterationMode, unrollFactor)
                : RunSpecific(invokeCount, iterationMode, targetCount.Value, unrollFactor);

        private List<Measurement> RunAuto(long invokeCount, IterationMode iterationMode, int unrollFactor)
        {
            var measurements = measurementsPool.Next();
            var measurementsForStatistics = measurementsPool.Next();

            int iterationCounter = 0;
            bool isIdle = iterationMode.IsIdle();
            double maxErrorRelative = isIdle ? MaxIdleStdErrRelative : maxStdErrRelative;
            while (true)
            {
                iterationCounter++;
                var measurement = RunIteration(iterationMode, iterationCounter, invokeCount, unrollFactor);
                measurements.Add(measurement);
                measurementsForStatistics.Add(measurement);

                var statistics = MeasurementsStatistics.Calculate(measurementsForStatistics, removeOutliers);
                double actualError = statistics.StandardError;
                double maxError = maxErrorRelative * statistics.Mean;

                if (iterationCounter >= MinIterationCount && actualError < maxError)
                    break;

                if (iterationCounter >= MaxIterationCount || (isIdle && iterationCounter >= MaxIdleIterationCount))
                    break;
            }
            if (!IsDiagnoserAttached) WriteLine();
            return measurements;
        }

        private List<Measurement> RunSpecific(long invokeCount, IterationMode iterationMode, int iterationCount, int unrollFactor)
        {
            var measurements = measurementsPool.Next();

            for (int i = 0; i < iterationCount; i++)
                measurements.Add(RunIteration(iterationMode, i + 1, invokeCount, unrollFactor));

            if (!IsDiagnoserAttached) WriteLine();

            return measurements;
        }
    }
}