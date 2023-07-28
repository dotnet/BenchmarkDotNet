using System;
using System.Collections.Generic;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Engines
{
    // TODO: use clockResolution
    internal class EnginePilotStage : EngineStage
    {
        public readonly struct PilotStageResult
        {
            public long PerfectInvocationCount { get; }
            public IReadOnlyList<Measurement> Measurements { get; }

            public PilotStageResult(long perfectInvocationCount, List<Measurement> measurements)
            {
                PerfectInvocationCount = perfectInvocationCount;
                Measurements = measurements;
            }

            public PilotStageResult(long perfectInvocationCount)
            {
                PerfectInvocationCount = perfectInvocationCount;
                Measurements = Array.Empty<Measurement>();
            }
        }

        internal const long MaxInvokeCount = (long.MaxValue / 2 + 1) / 2;

        private readonly int unrollFactor;
        private readonly TimeInterval minIterationTime;
        private readonly int minInvokeCount;
        private readonly double maxRelativeError;
        private readonly TimeInterval? maxAbsoluteError;
        private readonly double targetIterationTime;
        private readonly double resolution;

        public EnginePilotStage(IEngine engine) : base(engine)
        {
            unrollFactor = engine.TargetJob.ResolveValue(RunMode.UnrollFactorCharacteristic, engine.Resolver);
            minIterationTime = engine.TargetJob.ResolveValue(AccuracyMode.MinIterationTimeCharacteristic, engine.Resolver);
            minInvokeCount = engine.TargetJob.ResolveValue(AccuracyMode.MinInvokeCountCharacteristic, engine.Resolver);
            maxRelativeError = engine.TargetJob.ResolveValue(AccuracyMode.MaxRelativeErrorCharacteristic, engine.Resolver);
            maxAbsoluteError = engine.TargetJob.ResolveValueAsNullable(AccuracyMode.MaxAbsoluteErrorCharacteristic);
            targetIterationTime = engine.TargetJob.ResolveValue(RunMode.IterationTimeCharacteristic, engine.Resolver).ToNanoseconds();
            resolution =  engine.TargetJob.ResolveValue(InfrastructureMode.ClockCharacteristic, engine.Resolver).GetResolution().Nanoseconds;
        }

        /// <returns>Perfect invocation count</returns>
        public PilotStageResult Run()
        {
            // If InvocationCount is specified, pilot stage should be skipped
            if (TargetJob.HasValue(RunMode.InvocationCountCharacteristic))
                return new PilotStageResult(TargetJob.Run.InvocationCount);

            // Here we want to guess "perfect" amount of invocation
            return TargetJob.HasValue(RunMode.IterationTimeCharacteristic)
                ? RunSpecific()
                : RunAuto();
        }

        /// <summary>
        /// A case where we don't have specific iteration time.
        /// </summary>
        private PilotStageResult RunAuto()
        {
            long invokeCount = Autocorrect(minInvokeCount);
            var measurements = new List<Measurement>();

            int iterationCounter = 0;
            while (true)
            {
                iterationCounter++;
                var measurement = RunIteration(IterationMode.Workload, IterationStage.Pilot, iterationCounter, invokeCount, unrollFactor);
                measurements.Add(measurement);
                double iterationTime = measurement.Nanoseconds;
                double operationError = 2.0 * resolution / invokeCount; // An operation error which has arisen due to the Chronometer precision

                // Max acceptable operation error
                double operationMaxError1 = iterationTime / invokeCount * maxRelativeError;
                double operationMaxError2 = maxAbsoluteError?.Nanoseconds ?? double.MaxValue;
                double operationMaxError = Math.Min(operationMaxError1, operationMaxError2);

                bool isFinished = operationError < operationMaxError && iterationTime >= minIterationTime.Nanoseconds;
                if (isFinished)
                    break;
                if (invokeCount >= MaxInvokeCount)
                    break;

                if (unrollFactor == 1 && invokeCount < EnvironmentResolver.DefaultUnrollFactorForThroughput)
                    invokeCount += 1;
                else
                    invokeCount *= 2;
            }
            WriteLine();

            return new PilotStageResult(invokeCount, measurements);
        }

        /// <summary>
        /// A case where we have specific iteration time.
        /// </summary>
        private PilotStageResult RunSpecific()
        {
            long invokeCount = Autocorrect(Engine.MinInvokeCount);
            var measurements = new List<Measurement>();

            int iterationCounter = 0;

            int downCount = 0; // Amount of iterations where newInvokeCount < invokeCount
            while (true)
            {
                iterationCounter++;
                var measurement = RunIteration(IterationMode.Workload, IterationStage.Pilot, iterationCounter, invokeCount, unrollFactor);
                measurements.Add(measurement);
                double actualIterationTime = measurement.Nanoseconds;
                long newInvokeCount = Autocorrect(Math.Max(minInvokeCount, (long)Math.Round(invokeCount * targetIterationTime / actualIterationTime)));

                if (newInvokeCount < invokeCount)
                    downCount++;

                if (Math.Abs(newInvokeCount - invokeCount) <= 1 || downCount >= 3)
                    break;

                invokeCount = newInvokeCount;
            }
            WriteLine();

            return new PilotStageResult(invokeCount, measurements);
        }

        private long Autocorrect(long count) => (count + unrollFactor - 1) / unrollFactor * unrollFactor;
    }
}