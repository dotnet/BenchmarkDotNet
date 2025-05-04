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

    internal sealed class EngineActualStageAuto(Job targetJob, IResolver resolver, IterationMode iterationMode) : EngineActualStage(iterationMode)
    {
        private readonly double maxRelativeError = targetJob.ResolveValue(AccuracyMode.MaxRelativeErrorCharacteristic, resolver);
        private readonly TimeInterval? maxAbsoluteError = targetJob.ResolveValueAsNullable(AccuracyMode.MaxAbsoluteErrorCharacteristic);
        private readonly OutlierMode outlierMode = targetJob.ResolveValue(AccuracyMode.OutlierModeCharacteristic, resolver);
        private readonly int minIterationCount = targetJob.ResolveValue(RunMode.MinIterationCountCharacteristic, resolver);
        private readonly int maxIterationCount = targetJob.ResolveValue(RunMode.MaxIterationCountCharacteristic, resolver);
        private int _iterationCounter = 0;

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
            _iterationCounter++;

            var statistics = MeasurementsStatistics.Calculate(measurements, outlierMode);
            double actualError = statistics.LegacyConfidenceInterval.Margin;

            double maxError1 = effectiveMaxRelativeError * statistics.Mean;
            double maxError2 = maxAbsoluteError?.Nanoseconds ?? double.MaxValue;
            double maxError = Math.Min(maxError1, maxError2);

            if (_iterationCounter >= minIterationCount && actualError < maxError)
            {
                return false;
            }

            if (_iterationCounter >= maxIterationCount || isOverhead && _iterationCounter >= MaxOverheadIterationCount)
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