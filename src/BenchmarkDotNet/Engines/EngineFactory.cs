using System;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;

namespace BenchmarkDotNet.Engines
{
    public class EngineFactory : IEngineFactory
    {
        public IEngine CreateReadyToRun(EngineParameters engineParameters)
        {
            if (engineParameters.WorkloadActionNoUnroll == null)
                throw new ArgumentNullException(nameof(engineParameters.WorkloadActionNoUnroll));
            if (engineParameters.WorkloadActionUnroll == null)
                throw new ArgumentNullException(nameof(engineParameters.WorkloadActionUnroll));
            if (engineParameters.Dummy1Action == null)
                throw new ArgumentNullException(nameof(engineParameters.Dummy1Action));
            if (engineParameters.Dummy2Action == null)
                throw new ArgumentNullException(nameof(engineParameters.Dummy2Action));
            if (engineParameters.Dummy3Action == null)
                throw new ArgumentNullException(nameof(engineParameters.Dummy3Action));
            if (engineParameters.OverheadActionNoUnroll == null)
                throw new ArgumentNullException(nameof(engineParameters.OverheadActionNoUnroll));
            if (engineParameters.OverheadActionUnroll == null)
                throw new ArgumentNullException(nameof(engineParameters.OverheadActionUnroll));
            if(engineParameters.TargetJob == null)
                throw new ArgumentNullException(nameof(engineParameters.TargetJob));

            engineParameters.GlobalSetupAction?.Invoke(); // whatever the settings are, we MUST call global setup here, the global cleanup is part of Engine's Dispose

            if (!engineParameters.NeedsJitting) // just create the engine, do NOT jit
                return CreateMultiActionEngine(engineParameters);

            int jitIndex = 0;

            if (engineParameters.HasInvocationCount || engineParameters.HasUnrollFactor) // it's a job with explicit configuration, just create the engine and jit it
            {
                var warmedUpMultiActionEngine = CreateMultiActionEngine(engineParameters);

                DeadCodeEliminationHelper.KeepAliveWithoutBoxing(Jit(warmedUpMultiActionEngine, ++jitIndex, invokeCount: engineParameters.UnrollFactor, unrollFactor: engineParameters.UnrollFactor));

                return warmedUpMultiActionEngine;
            }

            var singleActionEngine = CreateSingleActionEngine(engineParameters);
            var singleInvocationTime = Jit(singleActionEngine, ++jitIndex, invokeCount: 1, unrollFactor: 1);

            if (singleInvocationTime > engineParameters.IterationTime)
                return singleActionEngine; // executing once takes longer than iteration time => long running benchmark, needs no pilot and no overhead

            int defaultUnrollFactor = Job.Default.ResolveValue(RunMode.UnrollFactorCharacteristic, EngineParameters.DefaultResolver);

            double timesPerIteration = engineParameters.IterationTime / singleInvocationTime; // how many times can we run given benchmark per iteration

            if (timesPerIteration < 1.5) // example: IterationTime is 0.5s, but single invocation takes 0.4s => we don't want to run it twice per iteration
                return singleActionEngine;

            int roundedUpTimesPerIteration = (int)Math.Ceiling(timesPerIteration);

            if (roundedUpTimesPerIteration < defaultUnrollFactor) // if we run it defaultUnrollFactor times per iteration, it's going to take longer than IterationTime
            {
                var needsPilot = engineParameters.TargetJob
                    .WithUnrollFactor(1) // we don't want to use unroll factor!
                    .WithMinInvokeCount(2) // the minimum is 2 (not the default 4 which can be too much and not 1 which we already know is not enough)
                    .WithEvaluateOverhead(false); // it's something very time consuming, it overhead is too small compared to total time

                return CreateEngine(engineParameters, needsPilot, engineParameters.OverheadActionNoUnroll, engineParameters.WorkloadActionNoUnroll);
            }

            var multiActionEngine = CreateMultiActionEngine(engineParameters);

            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(Jit(multiActionEngine, ++jitIndex, invokeCount: defaultUnrollFactor, unrollFactor: defaultUnrollFactor));

            return multiActionEngine;
        }

        /// <returns>the time it took to run the benchmark</returns>
        private static TimeInterval Jit(Engine engine, int jitIndex, int invokeCount, int unrollFactor)
        {
            engine.Dummy1Action.Invoke();

            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(engine.RunIteration(new IterationData(IterationMode.Overhead, IterationStage.Jitting, jitIndex, invokeCount, unrollFactor))); // don't forget to JIT idle

            engine.Dummy2Action.Invoke();

            var result = engine.RunIteration(new IterationData(IterationMode.Workload, IterationStage.Jitting, jitIndex, invokeCount, unrollFactor));

            engine.Dummy3Action.Invoke();

            engine.WriteLine();

            return TimeInterval.FromNanoseconds(result.Nanoseconds);
        }

        private static Engine CreateMultiActionEngine(EngineParameters engineParameters)
            => CreateEngine(engineParameters, engineParameters.TargetJob, engineParameters.OverheadActionUnroll, engineParameters.WorkloadActionUnroll);

        private static Engine CreateSingleActionEngine(EngineParameters engineParameters)
            => CreateEngine(engineParameters,
                engineParameters.TargetJob
                    .WithInvocationCount(1).WithUnrollFactor(1) // run the benchmark exactly once per iteration
                    .WithEvaluateOverhead(false), // it's something very time consuming, it overhead is too small compared to total time
                    // todo: consider if we should set the warmup count to 2
                engineParameters.OverheadActionNoUnroll,
                engineParameters.WorkloadActionNoUnroll);

        private static Engine CreateEngine(EngineParameters engineParameters, Job job, Action<long> idle, Action<long> main)
            => new Engine(
                engineParameters.Host,
                EngineParameters.DefaultResolver,
                engineParameters.Dummy1Action,
                engineParameters.Dummy2Action,
                engineParameters.Dummy3Action,
                idle,
                main,
                job,
                engineParameters.GlobalSetupAction,
                engineParameters.GlobalCleanupAction,
                engineParameters.IterationSetupAction,
                engineParameters.IterationCleanupAction,
                engineParameters.OperationsPerInvoke,
                engineParameters.MeasureExtraStats,
                engineParameters.Encoding,
                engineParameters.BenchmarkName);
    }
}