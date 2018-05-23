using System;
using BenchmarkDotNet.Characteristics;
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

            var resolver = new CompositeResolver(BenchmarkRunner.DefaultResolver, EngineResolver.Instance);
            var unrollFactor = engineParameters.TargetJob.ResolveValue(RunMode.UnrollFactorCharacteristic, resolver);
            
            engineParameters.GlobalSetupAction?.Invoke();

            var needsJitting = engineParameters.TargetJob.ResolveValue(RunMode.RunStrategyCharacteristic, resolver).NeedsJitting();
            if (!needsJitting)
            {
                // whatever it is, we can not interfere
                return CreateEngine(engineParameters, resolver, engineParameters.TargetJob, engineParameters.IdleMultiAction, engineParameters.MainMultiAction);
            }

            var needsPilot = !engineParameters.TargetJob.HasValue(RunMode.InvocationCountCharacteristic);
            var hasUnrollFactorDefined = engineParameters.TargetJob.HasValue(RunMode.UnrollFactorCharacteristic);
            
            if (needsPilot && !hasUnrollFactorDefined) 
            {
                var singleActionEngine = CreateEngine(engineParameters, resolver, engineParameters.TargetJob, engineParameters.IdleSingleAction, engineParameters.MainSingleAction);

                var iterationTime = resolver.Resolve(engineParameters.TargetJob, RunMode.IterationTimeCharacteristic);
                if (ShouldExecuteOncePerIteration(Jit(singleActionEngine, unrollFactor: 1), iterationTime))
                {
                    var reconfiguredJob = engineParameters.TargetJob.WithInvocationCount(1).WithUnrollFactor(1); // todo: consider if we should set the warmup count to 1!

                    return CreateEngine(engineParameters, resolver, reconfiguredJob, engineParameters.IdleSingleAction, engineParameters.MainSingleAction);
                }
            }

            // it's either a job with explicit configuration or not-very time consuming benchmark, just create the engine, Jit and return
            var multiActionEngine = CreateEngine(engineParameters, resolver, engineParameters.TargetJob, engineParameters.IdleMultiAction, engineParameters.MainMultiAction);
                
            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(Jit(multiActionEngine, unrollFactor));

            return multiActionEngine;
        }

        /// <summary>
        /// returns true if it takes longer than the desired iteration time (0,5s by default) to execute benchmark once
        /// </summary>
        private static bool ShouldExecuteOncePerIteration(Measurement jit, TimeInterval iterationTime)
            => TimeInterval.FromNanoseconds(jit.GetAverageNanoseconds()) > iterationTime;

        private static Measurement Jit(Engine engine, int unrollFactor)
        {
            engine.Dummy1Action.Invoke();

            DeadCodeEliminationHelper.KeepAliveWithoutBoxing(engine.RunIteration(new IterationData(IterationMode.IdleJitting, index: 1, invokeCount: unrollFactor, unrollFactor: unrollFactor))); // don't forget to JIT idle
            
            engine.Dummy2Action.Invoke();

            var result = engine.RunIteration(new IterationData(IterationMode.MainJitting, index: 1, invokeCount: unrollFactor, unrollFactor: unrollFactor));

            engine.Dummy3Action.Invoke();

            engine.WriteLine();
            
            return result;
        }

        private static Engine CreateEngine(EngineParameters engineParameters, IResolver resolver, Job job, Action<long> idle, Action<long> main)
            => new Engine(
                engineParameters.Host,
                resolver,
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