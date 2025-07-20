using System;
using System.Collections.Generic;
using System.Threading;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Engines
{
    internal abstract class EngineJitStage(EngineParameters parameters) : EngineStage(IterationStage.Jitting, IterationMode.Workload, parameters)
    {
        protected readonly Action<long> dummy1Action= _ => parameters.Dummy1Action();
        protected readonly Action<long> dummy2Action= _ => parameters.Dummy2Action();
        protected readonly Action<long> dummy3Action = _ => parameters.Dummy3Action();

        protected IterationData GetDummyIterationData(Action<long> dummyAction)
            => new(IterationMode.Dummy, IterationStage.Jitting, iterationIndex, 1, 1, () => { }, () => { }, dummyAction);
    }

    // We do our best to encourage the jit to fully promote methods to tier1, but tiered jit relies on heuristics,
    // and we purposefully don't spend too much time in this stage, so we can't guarantee it.
    // This should succeed for 99%+ of microbenchmarks. For any sufficiently short benchmarks where this fails,
    // the following stages (Pilot and Warmup) will likely take it the rest of the way. Long-running benchmarks may never fully reach tier1.
    internal sealed class EngineFirstJitStage : EngineJitStage
    {
        // It is not worth spending a long time in jit stage for macro-benchmarks.
        private static readonly TimeInterval MaxTieringTime = TimeInterval.FromSeconds(10);

        // Jit call counting delay is only for when the app starts up. We don't need to wait for every benchmark if multiple benchmarks are ran in-process.
        private static TimeSpan tieredDelay = JitInfo.TieredDelay;

        internal bool didStopEarly = false;
        internal Measurement lastMeasurement;

        private readonly IEnumerator<IterationData> enumerator;
        private readonly bool evaluateOverhead;

        internal EngineFirstJitStage(bool evaluateOverhead, EngineParameters parameters) : base(parameters)
        {
            enumerator = EnumerateIterations();
            this.evaluateOverhead = evaluateOverhead;
        }

        internal override List<Measurement> GetMeasurementList() => new(GetMaxMeasurementCount());

        private int GetMaxMeasurementCount()
        {
            if (!JitInfo.IsTiered)
            {
                return 1;
            }
            int count = JitInfo.MaxTierPromotions* JitInfo.TieredCallCountThreshold + 2;
            if (evaluateOverhead)
            {
                count *= 2;
            }
            return count;
        }

        internal override bool GetShouldRunIteration(List<Measurement> measurements, out IterationData iterationData)
        {
            if (measurements.Count > 0)
            {
                var measurement = measurements[measurements.Count - 1];
                if (measurement.IterationMode == IterationMode.Workload)
                {
                    lastMeasurement = measurement;
                }
            }
            if (enumerator.MoveNext())
            {
                iterationData = enumerator.Current;
                return true;
            }
            enumerator.Dispose();
            iterationData = default;
            return false;
        }

        private IEnumerator<IterationData> EnumerateIterations()
        {
            ++iterationIndex;
            if (evaluateOverhead)
            {
                yield return GetDummyIterationData(dummy1Action);
                yield return GetOverheadIterationData();
            }
            yield return GetDummyIterationData(dummy2Action);
            yield return GetWorkloadIterationData();
            yield return GetDummyIterationData(dummy3Action);

            // If the jit is not tiered, we're done.
            if (!JitInfo.IsTiered)
            {
                yield break;
            }

            // Wait enough time for jit call counting to begin.
            SleepHelper.SleepIfPositive(tieredDelay);
            // Don't make the next jit stage wait if it's ran in the same process.
            tieredDelay = TimeSpan.Zero;

            // Attempt to promote methods to tier1, but don't spend too much time in jit stage.
            StartedClock startedClock = parameters.TargetJob.ResolveValue(InfrastructureMode.ClockCharacteristic, parameters.Resolver).Start();

            int remainingTiers = JitInfo.MaxTierPromotions;
            while (remainingTiers > 0)
            {
                --remainingTiers;
                int remainingCalls = JitInfo.TieredCallCountThreshold;
                while (remainingCalls > 0)
                {
                    --remainingCalls;
                    ++iterationIndex;
                    if (evaluateOverhead)
                    {
                        yield return GetOverheadIterationData();
                    }
                    yield return GetWorkloadIterationData();

                    if ((remainingTiers + remainingCalls) > 0
                        && startedClock.GetElapsed().GetTimeValue() >= MaxTieringTime)
                    {
                        didStopEarly = true;
                        yield break;
                    }
                }

                SleepHelper.SleepIfPositive(JitInfo.BackgroundCompilationDelay);
            }

            // Empirical evidence shows that the first call after the method is tiered up may take longer,
            // so we run an extra iteration to ensure the next stage gets a stable measurement.
            ++iterationIndex;
            if (evaluateOverhead)
            {
                yield return GetOverheadIterationData();
            }
            yield return GetWorkloadIterationData();
        }

        private IterationData GetOverheadIterationData()
            => new(IterationMode.Overhead, IterationStage.Jitting, iterationIndex, 1, 1, () => { }, () => { }, parameters.OverheadActionNoUnroll);

        private IterationData GetWorkloadIterationData()
            => new(IterationMode.Workload, IterationStage.Jitting, iterationIndex, 1, 1, parameters.IterationSetupAction, parameters.IterationCleanupAction, parameters.WorkloadActionNoUnroll);
    }

    internal sealed class EngineSecondJitStage : EngineJitStage
    {
        private readonly int unrollFactor;
        private readonly bool evaluateOverhead;

        public EngineSecondJitStage(int unrollFactor, bool evaluateOverhead, EngineParameters parameters) : base(parameters)
        {
            this.unrollFactor = unrollFactor;
            this.evaluateOverhead = evaluateOverhead;
            iterationIndex = evaluateOverhead ? 0 : 2;
        }

        internal override List<Measurement> GetMeasurementList() => new(evaluateOverhead ? 5 : 3);

        // The benchmark method has already been jitted via *NoUnroll, we only need to jit the *Unroll methods here, which aren't tiered.
        internal override bool GetShouldRunIteration(List<Measurement> measurements, out IterationData iterationData)
        {
            iterationData = ++iterationIndex switch
            {
                1 => GetDummyIterationData(dummy1Action),
                2 => new(IterationMode.Overhead, IterationStage.Jitting, 1, unrollFactor, unrollFactor, () => { }, () => { }, parameters.OverheadActionUnroll),
                3 => GetDummyIterationData(dummy2Action),
                // IterationSetup/Cleanup are only used for *NoUnroll benchmarks
                4 => new(IterationMode.Workload, IterationStage.Jitting, 1, unrollFactor, unrollFactor, () => { }, () => { }, parameters.WorkloadActionUnroll),
                5 => GetDummyIterationData(dummy3Action),
                _ => default
            };
            return iterationIndex <= 5;
        }
    }
}