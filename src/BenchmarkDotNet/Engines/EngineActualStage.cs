using System;
using System.Collections.Generic;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;
using Perfolizer.Mathematics.OutlierDetection;

namespace BenchmarkDotNet.Engines
{
    internal abstract class EngineActualStage(IterationMode iterationMode, long invokeCount, int unrollFactor, EngineParameters parameters) : EngineStage(IterationStage.Actual, iterationMode, parameters)
    {
        internal const int MaxOverheadIterationCount = 20;

        internal readonly long invokeCount = invokeCount;
        internal readonly int unrollFactor = unrollFactor;

        internal static EngineActualStage GetOverhead(long invokeCount, int unrollFactor, EngineParameters parameters)
            => new EngineActualStageAuto(IterationMode.Overhead, invokeCount, unrollFactor, parameters);

        internal static EngineActualStage GetWorkload(RunStrategy strategy, long invokeCount, int unrollFactor, EngineParameters parameters)
        {
            var targetJob = parameters.TargetJob;
            int? iterationCount = targetJob.ResolveValueAsNullable(RunMode.IterationCountCharacteristic);
            const int DefaultWorkloadCount = 10;
            return iterationCount == null && strategy != RunStrategy.Monitoring
                ? new EngineActualStageAuto(IterationMode.Workload, invokeCount, unrollFactor, parameters)
                : new EngineActualStageSpecific(iterationCount ?? DefaultWorkloadCount, IterationMode.Workload, invokeCount, unrollFactor, parameters);
        }

        protected IterationData GetIterationData()
            => new(Mode, Stage, ++iterationIndex, invokeCount, unrollFactor, parameters.IterationSetupAction, parameters.IterationCleanupAction,
                Mode == IterationMode.Workload
                ? unrollFactor == 1 ? parameters.WorkloadActionNoUnroll : parameters.WorkloadActionUnroll
                : unrollFactor == 1 ? parameters.OverheadActionNoUnroll : parameters.OverheadActionUnroll);
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

        public EngineActualStageAuto(IterationMode iterationMode, long invokeCount, int unrollFactor, EngineParameters parameters) : base(iterationMode, invokeCount, unrollFactor, parameters)
        {
            maxRelativeError = parameters.TargetJob.ResolveValue(AccuracyMode.MaxRelativeErrorCharacteristic, parameters.Resolver);
            maxAbsoluteError = parameters.TargetJob.ResolveValueAsNullable(AccuracyMode.MaxAbsoluteErrorCharacteristic);
            outlierMode = parameters.TargetJob.ResolveValue(AccuracyMode.OutlierModeCharacteristic, parameters.Resolver);
            minIterationCount = parameters.TargetJob.ResolveValue(RunMode.MinIterationCountCharacteristic, parameters.Resolver);
            maxIterationCount = parameters.TargetJob.ResolveValue(RunMode.MaxIterationCountCharacteristic, parameters.Resolver);
            measurementsForStatistics = GetMeasurementList();
        }

        internal override List<Measurement> GetMeasurementList() => new(maxIterationCount);

        internal override bool GetShouldRunIteration(List<Measurement> measurements, out IterationData iterationData)
        {
            if (measurements.Count == 0)
            {
                iterationData = GetIterationData();
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
                iterationData = default;
                return false;
            }

            if (iterationCounter >= maxIterationCount || isOverhead && iterationCounter >= MaxOverheadIterationCount)
            {
                iterationData = default;
                return false;
            }

            iterationData = GetIterationData();
            return true;
        }
    }

    internal sealed class EngineActualStageSpecific(int maxIterationCount, IterationMode iterationMode, long invokeCount, int unrollFactor, EngineParameters parameters)
        : EngineActualStage(iterationMode, invokeCount, unrollFactor, parameters)
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