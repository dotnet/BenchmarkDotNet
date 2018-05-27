using System;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Engines
{
    public class EngineFactory : IEngineFactory
    {
        public IEngine CreateReadyToRun(EngineParameters engineParameters)
        {
            if (engineParameters.MainSingleAction == null)
                throw new ArgumentNullException(nameof(engineParameters.MainSingleAction));
            if (engineParameters.MainMultiAction == null)
                throw new ArgumentNullException(nameof(engineParameters.MainMultiAction));
            if (engineParameters.Dummy1Action == null)
                throw new ArgumentNullException(nameof(engineParameters.Dummy1Action));
            if (engineParameters.Dummy2Action == null)
                throw new ArgumentNullException(nameof(engineParameters.Dummy2Action));
            if (engineParameters.Dummy3Action == null)
                throw new ArgumentNullException(nameof(engineParameters.Dummy3Action));
            if (engineParameters.IdleSingleAction == null)
                throw new ArgumentNullException(nameof(engineParameters.IdleSingleAction));
            if (engineParameters.IdleMultiAction == null)
                throw new ArgumentNullException(nameof(engineParameters.IdleMultiAction));
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
            if (Jit(singleActionEngine, ++jitIndex, invokeCount: 1, unrollFactor: 1) > engineParameters.IterationTime)
                return singleActionEngine; // executing once takes longer than iteration time => long running benchmark, needs no pilot and no overhead

            var multiActionEngine = CreateMultiActionEngine(engineParameters);
            int defaultUnrollFactor = Job.Default.ResolveValue(RunMode.UnrollFactorCharacteristic, EngineParameters.DefaultResolver);

            if (Jit(multiActionEngine, ++jitIndex, invokeCount: defaultUnrollFactor, unrollFactor: defaultUnrollFactor) > engineParameters.IterationTime) 
            {   // executing defaultUnrollFactor times takes longer than iteration time => medium running benchmark, needs no pilot and no overhead
                var defaultUnrollFactorTimesPerIterationNoPilotNoOverhead = CreateJobWhichDoesNotNeedPilotAndOverheadEvaluation(engineParameters.TargetJob, 
                    invocationCount: defaultUnrollFactor, unrollFactor: defaultUnrollFactor); // run the benchmark exactly once per iteration
                
                return CreateEngine(engineParameters, defaultUnrollFactorTimesPerIterationNoPilotNoOverhead, engineParameters.IdleMultiAction, engineParameters.MainMultiAction);
            }
            
            return multiActionEngine;
        }

        /// <returns>the time it took to run the benchmark</returns>
        private static TimeInterval Jit(Engine engine, int jitIndex, int invokeCount, int unrollFactor)
        {
            engine.Dummy1Action.Invoke();

            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(engine.RunIteration(new IterationData(IterationMode.IdleJitting, jitIndex, invokeCount, unrollFactor))); // don't forget to JIT idle
            
            engine.Dummy2Action.Invoke();

            var result = engine.RunIteration(new IterationData(IterationMode.MainJitting, jitIndex, invokeCount, unrollFactor));

            engine.Dummy3Action.Invoke();

            engine.WriteLine();
            
            return TimeInterval.FromNanoseconds(result.Nanoseconds);
        }

        private static Engine CreateMultiActionEngine(EngineParameters engineParameters) 
            => CreateEngine(engineParameters, engineParameters.TargetJob, engineParameters.IdleMultiAction, engineParameters.MainMultiAction);

        private static Engine CreateSingleActionEngine(EngineParameters engineParameters) 
            => CreateEngine(engineParameters,
                CreateJobWhichDoesNotNeedPilotAndOverheadEvaluation(engineParameters.TargetJob, invocationCount: 1, unrollFactor: 1), // run the benchmark exactly once per iteration
                engineParameters.IdleSingleAction, 
                engineParameters.MainSingleAction);
        
        private static Job CreateJobWhichDoesNotNeedPilotAndOverheadEvaluation(Job sourceJob, int invocationCount, int unrollFactor)
            => sourceJob
                .WithInvocationCount(invocationCount).WithUnrollFactor(unrollFactor) 
                .WithEvaluateOverhead(false); // it's very time consuming, don't evaluate the overhead which would be 0,000025% of the target run or even less
                // todo: consider if we should set the warmup count to 2

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
                engineParameters.MeasureGcStats);
    }
}