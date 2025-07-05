using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Engines
{
    internal abstract class EngineStage(IterationStage stage, IterationMode mode, FrozenEngineParameters parameters)
    {
        internal readonly IterationStage Stage = stage;
        internal readonly IterationMode Mode = mode;
        protected readonly FrozenEngineParameters parameters = parameters;
        protected int iterationIndex;

        internal abstract List<Measurement> GetMeasurementList();
        internal abstract bool GetShouldRunIteration(List<Measurement> measurements, out IterationData iterationData);

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static IEnumerable<EngineStage> EnumerateStages(FrozenEngineParameters parameters)
        {
            var strategy = parameters.TargetJob.ResolveValue(RunMode.RunStrategyCharacteristic, parameters.Resolver);
            var invokeCount = parameters.TargetJob.ResolveValue(RunMode.InvocationCountCharacteristic, parameters.Resolver, 1);
            var unrollFactor = parameters.TargetJob.ResolveValue(RunMode.UnrollFactorCharacteristic, parameters.Resolver);

            if (strategy != RunStrategy.ColdStart)
            {
                if (strategy != RunStrategy.Monitoring)
                {
                    // If InvocationCount is specified, pilot stage should be skipped
                    bool skipPilotStage = parameters.TargetJob.HasValue(RunMode.InvocationCountCharacteristic);
                    bool evaluateOverhead = parameters.TargetJob.ResolveValue(AccuracyMode.EvaluateOverheadCharacteristic, parameters.Resolver);
                    int minInvokeCount = parameters.TargetJob.ResolveValue(AccuracyMode.MinInvokeCountCharacteristic, parameters.Resolver);

                    // AOT has no JIT.
                    if (!RuntimeInformation.IsAot)
                    {
                        var jitStage = new EngineFirstJitStage(parameters, unrollFactor);
                        yield return jitStage;

                        bool hasUnrollFactor = parameters.TargetJob.HasValue(RunMode.UnrollFactorCharacteristic);
                        if (!hasUnrollFactor && !skipPilotStage)
                        {
                            // Initial pilot stage adjusts unrollFactor from a single invocation.
                            var pilotStage = new EnginePilotAfterJitStage(parameters, invokeCount, unrollFactor, minInvokeCount);
                            // If the first jit invocation was too time consuming, just correct the values without running another invocation.
                            var firstMeasurement = jitStage.FirstMeasurement;
                            long invocations = firstMeasurement.Operations / parameters.OperationsPerInvoke;
                            var firstTime = TimeInterval.FromNanoseconds(firstMeasurement.Nanoseconds / invocations);
                            if (firstTime.ToSeconds() >= EngineFirstJitStage.MaxFirstCallSeconds)
                            {
                                pilotStage.CorrectValues(firstTime.Nanoseconds);
                            }
                            else
                            {
                                yield return pilotStage;
                            }

                            invokeCount = pilotStage.invokeCount;
                            unrollFactor = pilotStage.unrollFactor;
                            minInvokeCount = pilotStage.minInvokeCount;
                            evaluateOverhead &= pilotStage.evaluateOverhead;
                            skipPilotStage = !pilotStage.needsFurtherPilot;
                        }

                        // If the first jit stage only jitted *NoUnroll methods, now we need to jit *Unroll methods if they're going to be used.
                        if (!jitStage.didJitUnroll && unrollFactor != 1)
                        {
                            yield return new EngineSecondJitStage(parameters);
                        }
                    }
                    else if (!skipPilotStage)
                    {
                        // Initial pilot stage runs a single iteration to adjust unrollFactor.
                        var pilotStage = new EnginePilotAfterJitStage(parameters, invokeCount, unrollFactor, minInvokeCount);
                        yield return pilotStage;

                        invokeCount = pilotStage.invokeCount;
                        unrollFactor = pilotStage.unrollFactor;
                        minInvokeCount = pilotStage.minInvokeCount;
                        evaluateOverhead &= pilotStage.evaluateOverhead;
                        skipPilotStage = !pilotStage.needsFurtherPilot;
                    }

                    if (!skipPilotStage)
                    {
                        var pilotStage = EnginePilotStage.GetStage(parameters, invokeCount, unrollFactor, minInvokeCount);
                        yield return pilotStage;

                        invokeCount = pilotStage.invokeCount;
                    }

                    if (evaluateOverhead)
                    {
                        yield return EngineWarmupStage.GetOverhead(invokeCount, unrollFactor, parameters);
                        yield return EngineActualStage.GetOverhead(invokeCount, unrollFactor, parameters);
                    }
                }

                yield return EngineWarmupStage.GetWorkload(strategy, invokeCount, unrollFactor, parameters);

                // TODO: restart pilot/warmup stages if some heuristic determines it's necessary (#2787, #1210).
            }

            yield return EngineActualStage.GetWorkload(strategy, invokeCount, unrollFactor, parameters);
        }
    }
}