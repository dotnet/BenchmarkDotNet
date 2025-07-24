using System;
using System.Collections.Generic;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Engines
{
    // TODO: use clockResolution
    internal abstract class EnginePilotStage(long invokeCount, int unrollFactor, int minInvokeCount, EngineParameters parameters) : EngineStage(IterationStage.Pilot, IterationMode.Workload, parameters)
    {
        internal const long MaxInvokeCount = (long.MaxValue / 2 + 1) / 2;

        internal long invokeCount = invokeCount;
        internal int unrollFactor = unrollFactor;
        internal int minInvokeCount = minInvokeCount;

        internal override List<Measurement> GetMeasurementList() => [];

        internal static EnginePilotStage GetStage(long invokeCount, int unrollFactor, int minInvokeCount, EngineParameters parameters)
            // Here we want to guess "perfect" amount of invocation
            => parameters.TargetJob.HasValue(RunMode.IterationTimeCharacteristic)
                ? new EnginePilotStageSpecific(invokeCount, unrollFactor, minInvokeCount, parameters)
                : new EnginePilotStageAuto(invokeCount, unrollFactor, minInvokeCount, parameters);

        protected long Autocorrect(long count) => (count + unrollFactor - 1) / unrollFactor * unrollFactor;

        protected IterationData GetIterationData()
            => new(Mode, Stage, ++iterationIndex, invokeCount, unrollFactor, parameters.IterationSetupAction, parameters.IterationCleanupAction,
                unrollFactor == 1 ? parameters.WorkloadActionNoUnroll : parameters.WorkloadActionUnroll);
    }

    internal sealed class EnginePilotStageInitial(long invokeCount, int unrollFactor, int minInvokeCount, EngineParameters parameters) : EnginePilotStage(invokeCount, unrollFactor, minInvokeCount, parameters)
    {
        internal bool evaluateOverhead = true;
        internal bool needsFurtherPilot = true;

        internal override List<Measurement> GetMeasurementList() => [];

        internal override bool GetShouldRunIteration(List<Measurement> measurements, out IterationData iterationData)
        {
            if (measurements.Count == 0)
            {
                iterationData = new(Mode, Stage, ++iterationIndex, 1, 1, parameters.IterationSetupAction, parameters.IterationCleanupAction, parameters.WorkloadActionNoUnroll);
                return true;
            }

            CorrectValues(measurements[measurements.Count - 1]);
            iterationData = default;
            return false;
        }

        internal void CorrectValues(Measurement measurement)
        {
            var iterationTime = parameters.TargetJob.ResolveValue(RunMode.IterationTimeCharacteristic, parameters.Resolver);
            var singleInvokeNanoseconds = measurement.Nanoseconds * parameters.OperationsPerInvoke / measurement.Operations;
            double timesPerIteration = iterationTime.Nanoseconds / singleInvokeNanoseconds; // how many times can we run given benchmark per iteration
            // Executing once takes longer than iteration time -> long running benchmark,
            // or executing twice would put us well past the iteration time.
            if (timesPerIteration < 1.5)
            {
                invokeCount = 1;
                unrollFactor = 1;
                // It's very time consuming, overhead is too small compared to total time.
                evaluateOverhead = false;
                needsFurtherPilot = false;
                return;
            }

            int roundedUpTimesPerIteration = (int) Math.Ceiling(timesPerIteration);
            // If we run it unrollFactor times per iteration, it's going to take longer than IterationTime.
            if (roundedUpTimesPerIteration < unrollFactor)
            {
                unrollFactor = 1;
                // The minimum is 2 (not the default 4 which can be too much and not 1 which we already know is not enough).
                minInvokeCount = 2;
                // It's very time consuming, overhead is too small compared to total time.
                evaluateOverhead = false;
            }
        }
    }

    internal sealed class EnginePilotStageAuto(long invokeCount, int unrollFactor, int minInvokeCount, EngineParameters parameters) : EnginePilotStage(invokeCount, unrollFactor, minInvokeCount, parameters)
    {
        private readonly TimeInterval minIterationTime = parameters.TargetJob.ResolveValue(AccuracyMode.MinIterationTimeCharacteristic, parameters.Resolver);
        private readonly double maxRelativeError = parameters.TargetJob.ResolveValue(AccuracyMode.MaxRelativeErrorCharacteristic, parameters.Resolver);
        private readonly TimeInterval? maxAbsoluteError = parameters.TargetJob.ResolveValueAsNullable(AccuracyMode.MaxAbsoluteErrorCharacteristic);
        private readonly double resolution = parameters.TargetJob.ResolveValue(InfrastructureMode.ClockCharacteristic, parameters.Resolver).GetResolution().Nanoseconds;

        internal override bool GetShouldRunIteration(List<Measurement> measurements, out IterationData iterationData)
        {
            if (measurements.Count == 0)
            {
                invokeCount = Autocorrect(minInvokeCount);
                iterationData = GetIterationData();
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
                iterationData = default;
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

                iterationData = GetIterationData();
            return true;
        }
    }

    internal sealed class EnginePilotStageSpecific(long invokeCount, int unrollFactor, int minInvokeCount, EngineParameters parameters) : EnginePilotStage(invokeCount, unrollFactor, minInvokeCount, parameters)
    {
        private readonly double targetIterationTime = parameters.TargetJob.ResolveValue(RunMode.IterationTimeCharacteristic, parameters.Resolver).ToNanoseconds();

        private int _downCount = 0; // Amount of iterations where newInvokeCount < invokeCount

        internal override bool GetShouldRunIteration(List<Measurement> measurements, out IterationData iterationData)
        {
            if (measurements.Count == 0)
            {
                invokeCount = Autocorrect(minInvokeCount);
                iterationData = GetIterationData();
                return true;
            }

            var measurement = measurements[measurements.Count - 1];
            double actualIterationTime = measurement.Nanoseconds;
            long newInvokeCount = Autocorrect(Math.Max(minInvokeCount, (long) Math.Round(invokeCount * targetIterationTime / actualIterationTime)));

            if (newInvokeCount < invokeCount)
            {
                _downCount++;
            }

            long diff = newInvokeCount - invokeCount;
            if (_downCount >= 3
                || (diff != long.MinValue && Math.Abs(diff) <= 1))
            {
                iterationData = default;
                return false;
            }

            invokeCount = newInvokeCount;
            iterationData = GetIterationData();
            return true;
        }
    }
}