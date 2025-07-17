using System;
using System.Collections.Generic;
using System.Threading;
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

        internal bool didStopEarly = false;
        internal Measurement lastMeasurement;

        private readonly IEnumerator<IterationData> enumerator;

        internal EngineFirstJitStage(EngineParameters parameters) : base(parameters)
        {
            enumerator = EnumerateIterations();
        }

        internal override List<Measurement> GetMeasurementList() => new(GetMaxMeasurementCount());

        private static int GetMaxMeasurementCount()
        {
            int tieredCallCountThreshold = JitInfo.TieredCallCountThreshold;
            if (JitInfo.IsDPGO)
            {
                tieredCallCountThreshold *= 2;
            }
            // +1 for first jit, x2 for overhead + workload
            return (tieredCallCountThreshold + 1) * 2;
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
            yield return GetDummyIterationData(dummy1Action);
            yield return GetOverheadIterationData();
            yield return GetDummyIterationData(dummy2Action);
            yield return GetWorkloadIterationData();
            yield return GetDummyIterationData(dummy3Action);

            // If the jit is not tiered, we're done.
            if (!JitInfo.IsTiered)
            {
                yield break;
            }

            // Wait enough time for jit call counting to begin.
            MaybeSleep(JitInfo.TieredDelay);

            // Attempt to promote methods to tier1, but don't spend too much time in jit stage.
            StartedClock startedClock = parameters.TargetJob.ResolveValue(InfrastructureMode.ClockCharacteristic, parameters.Resolver).Start();

            int remainingTiers = JitInfo.IsDPGO ? 2 : 1;
            while (remainingTiers > 0)
            {
                --remainingTiers;
                int remainingCalls = JitInfo.TieredCallCountThreshold;
                while (remainingCalls > 0)
                {
                    --remainingCalls;
                    ++iterationIndex;
                    yield return GetOverheadIterationData();
                    yield return GetWorkloadIterationData();

                    if ((remainingTiers + remainingCalls) > 0
                        && startedClock.GetElapsed().GetTimeValue() >= MaxTieringTime)
                    {
                        didStopEarly = true;
                        yield break;
                    }
                }

                MaybeSleep(JitInfo.BackgroundCompilationDelay);
            }

            // Empirical evidence shows that the first call after the method is tiered up takes longer,
            // so we run an extra iteration to ensure the next stage gets a stable measurement.
            ++iterationIndex;
            yield return GetOverheadIterationData();
            yield return GetWorkloadIterationData();
        }

        private IterationData GetOverheadIterationData()
            => new(IterationMode.Overhead, IterationStage.Jitting, iterationIndex, 1, 1, () => { }, () => { }, parameters.OverheadActionNoUnroll);

        private IterationData GetWorkloadIterationData()
            => new(IterationMode.Workload, IterationStage.Jitting, iterationIndex, 1, 1, parameters.IterationSetupAction, parameters.IterationCleanupAction, parameters.WorkloadActionNoUnroll);

        private static void MaybeSleep(TimeSpan timeSpan)
        {
            if (timeSpan > TimeSpan.Zero)
            {
                Thread.Sleep(timeSpan);
            }
        }
    }

    internal sealed class EngineSecondJitStage(int unrollFactor, EngineParameters parameters) : EngineJitStage(parameters)
    {
        private readonly int unrollFactor = unrollFactor;

        internal override List<Measurement> GetMeasurementList() => new(GetMaxCallCount());

        private static int GetMaxCallCount()
        {
            int tieredCallCountThreshold = JitInfo.TieredCallCountThreshold;
            if (JitInfo.IsDPGO)
            {
                tieredCallCountThreshold *= 2;
            }
            return tieredCallCountThreshold + 1;
        }

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