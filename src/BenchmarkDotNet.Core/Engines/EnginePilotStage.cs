using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Engines
{
    internal class EnginePilotStage : EngineStage
    {
        internal const long MaxInvokeCount = (long.MaxValue / 2 + 1) / 2;

        private readonly int unrollFactor;
        private readonly int minInvokeCount;
        private readonly double maxStdErrRelative;
        private readonly double targetIterationTime;
        private readonly TimeInterval clockResolution;
        private readonly double resolution;

        public EnginePilotStage(IEngine engine) : base(engine)
        {
            unrollFactor = engine.TargetJob.Run.UnrollFactor.Resolve(engine.Resolver);
            minInvokeCount = engine.TargetJob.Accuracy.MinInvokeCount.Resolve(engine.Resolver);
            maxStdErrRelative = engine.TargetJob.Accuracy.MaxStdErrRelative.Resolve(engine.Resolver);
            targetIterationTime = engine.TargetJob.Run.IterationTime.Resolve(engine.Resolver).ToNanoseconds();
            resolution =  engine.TargetJob.Infrastructure.Clock.Resolve(engine.Resolver).GetResolution().Nanoseconds;
        }

        /// <returns>Perfect invocation count</returns>
        public long Run()
        {
            // If InvocationCount is specified, pilot stage should be skipped
            if (!TargetJob.Run.InvocationCount.IsDefault)
                return TargetJob.Run.InvocationCount.SpecifiedValue;

            // Here we want to guess "perfect" amount of invocation
            return TargetJob.Run.IterationTime.IsDefault ? RunAuto() : RunSpecific();
        }

        /// <summary>
        /// A case where we don't have specific iteration time.
        /// </summary>
        private long RunAuto()
        {
            Func<long, long> autocorrect = count => (count + unrollFactor - 1) / unrollFactor * unrollFactor;

            long invokeCount = autocorrect(minInvokeCount);
            double maxError = maxStdErrRelative; // TODO: introduce a StdErr factor
            double minIterationTime = Engine.MinIterationTime.Nanoseconds;

            int iterationCounter = 0;
            while (true)
            {
                iterationCounter++;
                var measurement = RunIteration(IterationMode.Pilot, iterationCounter, invokeCount, unrollFactor);
                double iterationTime = measurement.Nanoseconds;
                double operationError = 2.0 * resolution / invokeCount; // An operation error which has arisen due to the Chronometer precision
                double operationMaxError = iterationTime / invokeCount * maxError; // Max acceptable operation error

                bool isFinished = operationError < operationMaxError && iterationTime >= minIterationTime;
                if (isFinished)
                    break;
                if (invokeCount >= MaxInvokeCount)
                    break;

                invokeCount *= 2;
            }
            if (!IsDiagnoserAttached) WriteLine();

            return invokeCount;
        }

        /// <summary>
        /// A case where we have specific iteration time.
        /// </summary>
        private long RunSpecific()
        {
            Func<long, long> autocorrect = count => (count + unrollFactor - 1) / unrollFactor * unrollFactor;

            long invokeCount = autocorrect(Engine.MinInvokeCount);

            int iterationCounter = 0;

            int downCount = 0; // Amount of iterations where newInvokeCount < invokeCount
            while (true)
            {
                iterationCounter++;
                var measurement = RunIteration(IterationMode.Pilot, iterationCounter, invokeCount, unrollFactor);
                double actualIterationTime = measurement.Nanoseconds;
                long newInvokeCount = autocorrect(Math.Max(minInvokeCount, (long)Math.Round(invokeCount * targetIterationTime / actualIterationTime)));

                if (newInvokeCount < invokeCount)
                    downCount++;

                if (Math.Abs(newInvokeCount - invokeCount) <= 1 || downCount >= 3)
                    break;

                invokeCount = newInvokeCount;
            }
            if (!IsDiagnoserAttached) WriteLine();

            return invokeCount;
        }
    }
}