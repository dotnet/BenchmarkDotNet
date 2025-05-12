using System;
using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;
using Perfolizer.Mathematics.OutlierDetection;

namespace BenchmarkDotNet.Engines
{
    internal abstract class EngineActualStage(IterationMode iterationMode) : EngineStage(IterationStage.Actual, iterationMode)
    {
        internal const int MaxOverheadIterationCount = 20;

        internal static EngineActualStage GetOverhead(IEngine engine)
            => new EngineActualStageAuto(engine.TargetJob, engine.Resolver, IterationMode.Overhead);

        internal static EngineActualStage GetWorkload(IEngine engine, RunStrategy strategy)
        {
            var targetJob = engine.TargetJob;
            int? iterationCount = targetJob.ResolveValueAsNullable(RunMode.IterationCountCharacteristic);
            const int DefaultWorkloadCount = 10;
            return iterationCount == null && strategy != RunStrategy.Monitoring
                ?  new EngineActualStageAuto(targetJob, engine.Resolver, IterationMode.Workload)
                :  new EngineActualStageSpecific(iterationCount ?? DefaultWorkloadCount, IterationMode.Workload);
        }
    }

    internal sealed class EngineActualStageAuto : EngineActualStage
    {
        private readonly double maxRelativeError;
        private readonly TimeInterval? maxAbsoluteError;
        private readonly OutlierMode outlierMode;
        private readonly int minIterationCount;
        private readonly int maxIterationCount;
        private readonly List<Measurement> measurementsForStatistics;
        private int iterationCounter = 0;

        public EngineActualStageAuto(Job targetJob, IResolver resolver, IterationMode iterationMode) : base(iterationMode)
        {
            maxRelativeError = targetJob.ResolveValue(AccuracyMode.MaxRelativeErrorCharacteristic, resolver);
            maxAbsoluteError = targetJob.ResolveValueAsNullable(AccuracyMode.MaxAbsoluteErrorCharacteristic);
            outlierMode = targetJob.ResolveValue(AccuracyMode.OutlierModeCharacteristic, resolver);
            minIterationCount = targetJob.ResolveValue(RunMode.MinIterationCountCharacteristic, resolver);
            maxIterationCount = targetJob.ResolveValue(RunMode.MaxIterationCountCharacteristic, resolver);
            measurementsForStatistics = GetMeasurementList();
        }

        internal override List<Measurement> GetMeasurementList() => new (maxIterationCount);

        internal override bool GetShouldRunIteration(List<Measurement> measurements, ref long invokeCount)
        {
            if (measurements.Count == 0)
            {
                return true;
            }

            const double MaxOverheadRelativeError = 0.05;
            bool isOverhead = Mode == IterationMode.Overhead;
            double effectiveMaxRelativeError = isOverhead ? MaxOverheadRelativeError : maxRelativeError;
            iterationCounter++;
            var measurement = measurements[measurements.Count - 1];
            measurementsForStatistics.Add(measurement);

            var statistics = MeasurementsStatistics.Calculate(measurementsForStatistics, outlierMode);
            double actualError = statistics.LegacyConfidenceInterval.Margin;

            double maxError1 = effectiveMaxRelativeError * statistics.Mean;
            double maxError2 = maxAbsoluteError?.Nanoseconds ?? double.MaxValue;
            double maxError = Math.Min(maxError1, maxError2);

            if (iterationCounter >= minIterationCount && actualError < maxError)
            {
                return false;
            }

            if (iterationCounter >= maxIterationCount || isOverhead && iterationCounter >= MaxOverheadIterationCount)
            {
                return false;
            }

            return true;
        }
    }

    internal sealed class EngineActualStageSpecific(int maxIterationCount, IterationMode iterationMode) : EngineActualStage(iterationMode)
    {
        private int iterationCount = 0;

        internal override List<Measurement> GetMeasurementList() => new (maxIterationCount);

        internal override bool GetShouldRunIteration(List<Measurement> measurements, ref long invokeCount)
            => ++iterationCount <= maxIterationCount;
    }
}