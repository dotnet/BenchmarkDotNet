using System;
using System.Collections.Generic;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Engines
{
    // TODO: use clockResolution
    internal abstract class EnginePilotStage(Job targetJob, IResolver resolver) : EngineStage(IterationStage.Pilot, IterationMode.Workload)
    {
        internal const long MaxInvokeCount = (long.MaxValue / 2 + 1) / 2;

        protected readonly int unrollFactor = targetJob.ResolveValue(RunMode.UnrollFactorCharacteristic, resolver);
        protected readonly int minInvokeCount = targetJob.ResolveValue(AccuracyMode.MinInvokeCountCharacteristic, resolver);

        protected long Autocorrect(long count) => (count + unrollFactor - 1) / unrollFactor * unrollFactor;

        internal static EnginePilotStage GetStage(IEngine engine)
        {
            var targetJob = engine.TargetJob;
            // If InvocationCount is specified, pilot stage should be skipped
            return targetJob.HasValue(RunMode.InvocationCountCharacteristic) ? null
                // Here we want to guess "perfect" amount of invocation
                : targetJob.HasValue(RunMode.IterationTimeCharacteristic) ? new EnginePilotStageSpecific(targetJob, engine.Resolver)
                : new EnginePilotStageAuto(targetJob, engine.Resolver);
        }
    }

    internal sealed class EnginePilotStageAuto(Job targetJob, IResolver resolver) : EnginePilotStage(targetJob, resolver)
    {
        private readonly TimeInterval minIterationTime = targetJob.ResolveValue(AccuracyMode.MinIterationTimeCharacteristic, resolver);
        private readonly double maxRelativeError = targetJob.ResolveValue(AccuracyMode.MaxRelativeErrorCharacteristic, resolver);
        private readonly TimeInterval? maxAbsoluteError = targetJob.ResolveValueAsNullable(AccuracyMode.MaxAbsoluteErrorCharacteristic);
        private readonly double resolution = targetJob.ResolveValue(InfrastructureMode.ClockCharacteristic, resolver).GetResolution().Nanoseconds;

        internal override List<Measurement> GetMeasurementList() => [];

        internal override bool GetShouldRunIteration(List<Measurement> measurements, ref long invokeCount)
        {
            if (measurements.Count == 0)
            {
                invokeCount = Autocorrect(minInvokeCount);
                return true;
            }

            var measurement = measurements[measurements.Count - 1];
            double iterationTime = measurement.Nanoseconds;
            double operationError = 2.0 * resolution / invokeCount; // An operation error which has arisen due to the Chronometer precision

            // Max acceptable operation error
            double operationMaxError1 = iterationTime / invokeCount * maxRelativeError;
            double operationMaxError2 = maxAbsoluteError?.Nanoseconds ?? double.MaxValue;
            double operationMaxError = Math.Min(operationMaxError1, operationMaxError2);

            bool isFinished = operationError < operationMaxError && iterationTime >= minIterationTime.Nanoseconds;
            if (isFinished || invokeCount >= MaxInvokeCount)
            {
                return false;
            }

            if (unrollFactor == 1 && invokeCount < EnvironmentResolver.DefaultUnrollFactorForThroughput)
            {
                ++invokeCount;
            }
            else
            {
                invokeCount *= 2;
            }

            return true;
        }
    }

    internal sealed class EnginePilotStageSpecific(Job targetJob, IResolver resolver) : EnginePilotStage(targetJob, resolver)
    {
        private const int MinInvokeCount = 4;

        private readonly double targetIterationTime = targetJob.ResolveValue(RunMode.IterationTimeCharacteristic, resolver).ToNanoseconds();

        private int _downCount = 0; // Amount of iterations where newInvokeCount < invokeCount

        internal override List<Measurement> GetMeasurementList() => [];

        internal override bool GetShouldRunIteration(List<Measurement> measurements, ref long invokeCount)
        {
            if (measurements.Count == 0)
            {
                invokeCount = Autocorrect(MinInvokeCount);
                return true;
            }

            var measurement = measurements[measurements.Count - 1];
            double actualIterationTime = measurement.Nanoseconds;
            long newInvokeCount = Autocorrect(Math.Max(minInvokeCount, (long) Math.Round(invokeCount * targetIterationTime / actualIterationTime)));

            if (newInvokeCount < invokeCount)
            {
                _downCount++;
            }

            if (Math.Abs(newInvokeCount - invokeCount) <= 1 || _downCount >= 3)
            {
                return false;
            }

            invokeCount = newInvokeCount;
            return true;
        }
    }
}