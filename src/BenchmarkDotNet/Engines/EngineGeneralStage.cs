using System;
using System.Collections.Generic;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;
using Perfolizer.Mathematics.OutlierDetection;

namespace BenchmarkDotNet.Engines
{
    public class EngineActualStage : EngineStage
    {
        internal const int MaxOverheadIterationCount = 20;
        internal const double MaxOverheadRelativeError = 0.05;
        internal const int DefaultWorkloadCount = 10;

        internal readonly int? iterationCount;
        internal readonly double maxRelativeError;
        internal readonly TimeInterval? maxAbsoluteError;
        internal readonly OutlierMode outlierMode;
        internal readonly int minIterationCount;
        internal readonly int maxIterationCount;

        public EngineActualStage(IEngine engine) : base(engine)
        {
            iterationCount = engine.TargetJob.ResolveValueAsNullable(RunMode.IterationCountCharacteristic);
            maxRelativeError = engine.TargetJob.ResolveValue(AccuracyMode.MaxRelativeErrorCharacteristic, engine.Resolver);
            maxAbsoluteError = engine.TargetJob.ResolveValueAsNullable(AccuracyMode.MaxAbsoluteErrorCharacteristic);
            outlierMode = engine.TargetJob.ResolveValue(AccuracyMode.OutlierModeCharacteristic, engine.Resolver);
            minIterationCount = engine.TargetJob.ResolveValue(RunMode.MinIterationCountCharacteristic, engine.Resolver);
            maxIterationCount = engine.TargetJob.ResolveValue(RunMode.MaxIterationCountCharacteristic, engine.Resolver);
        }

        public IReadOnlyList<Measurement> RunOverhead(long invokeCount, int unrollFactor)
            => RunAuto(invokeCount, IterationMode.Overhead, unrollFactor);

        public IReadOnlyList<Measurement> RunWorkload(long invokeCount, int unrollFactor, bool forceSpecific = false)
            => Run(invokeCount, IterationMode.Workload, false, unrollFactor, forceSpecific);

        internal IReadOnlyList<Measurement> Run(long invokeCount, IterationMode iterationMode, bool runAuto, int unrollFactor, bool forceSpecific = false)
            => (runAuto || iterationCount == null) && !forceSpecific
                ? RunAuto(invokeCount, iterationMode, unrollFactor)
                : RunSpecific(invokeCount, iterationMode, iterationCount ?? DefaultWorkloadCount, unrollFactor);

        private List<Measurement> RunAuto(long invokeCount, IterationMode iterationMode, int unrollFactor)
        {
            var measurements = new List<Measurement>(maxIterationCount);
            var measurementsForStatistics = new List<Measurement>(maxIterationCount);

            int iterationCounter = 0;
            bool isOverhead = iterationMode == IterationMode.Overhead;
            double effectiveMaxRelativeError = isOverhead ? MaxOverheadRelativeError : maxRelativeError;
            while (true)
            {
                iterationCounter++;
                var measurement = RunIteration(iterationMode, IterationStage.Actual, iterationCounter, invokeCount, unrollFactor);
                measurements.Add(measurement);
                measurementsForStatistics.Add(measurement);

                var statistics = MeasurementsStatistics.Calculate(measurementsForStatistics, outlierMode);
                double actualError = statistics.LegacyConfidenceInterval.Margin;

                double maxError1 = effectiveMaxRelativeError * statistics.Mean;
                double maxError2 = maxAbsoluteError?.Nanoseconds ?? double.MaxValue;
                double maxError = Math.Min(maxError1, maxError2);

                if (iterationCounter >= minIterationCount && actualError < maxError)
                    break;

                if (iterationCounter >= maxIterationCount || isOverhead && iterationCounter >= MaxOverheadIterationCount)
                    break;
            }
            WriteLine();

            return measurements;
        }

        private List<Measurement> RunSpecific(long invokeCount, IterationMode iterationMode, int iterationCount, int unrollFactor)
        {
            var measurements = new List<Measurement>(iterationCount);

            for (int i = 0; i < iterationCount; i++)
                measurements.Add(RunIteration(iterationMode, IterationStage.Actual, i + 1, invokeCount, unrollFactor));

            WriteLine();

            return measurements;
        }
    }
}