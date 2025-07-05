using System;
using System.Collections.Generic;
using System.Threading;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Engines
{
    internal abstract class EngineJitStage(FrozenEngineParameters parameters) : EngineStage(IterationStage.Jitting, IterationMode.Workload, parameters)
    {
        protected readonly Action<long> dummy1Action= _ => parameters.Dummy1Action();
        protected readonly Action<long> dummy2Action= _ => parameters.Dummy2Action();
        protected readonly Action<long> dummy3Action = _ => parameters.Dummy3Action();

        protected IterationData GetDummyIterationData(Action<long> dummyAction)
            => new(IterationMode.Dummy, IterationStage.Jitting, iterationIndex, 1, 1, () => { }, () => { }, dummyAction);
    }

    internal sealed class EngineFirstJitStage : EngineJitStage
    {
        // A magic number based on observations of reported bugs.
        internal const int MaxFirstCallSeconds = 10;
        // A magic number that should be enough for most microbenchmarks, but not too large to spend excess time in jit stage for macrobenchmarks.
        internal const int MaxJitStageSeconds = 15;

        internal bool didJitUnroll = false;
        private readonly int unrollFactor;
        private readonly Action<long> overheadAction;
        private readonly Action<long> workloadAction;
        private readonly IEnumerator<IterationData> enumerator;
        private Measurement? firstMeasurement;

        internal Measurement FirstMeasurement => firstMeasurement.Value;

        internal EngineFirstJitStage(FrozenEngineParameters parameters, int unrollFactor) : base(parameters)
        {
            this.unrollFactor = unrollFactor;
            if (unrollFactor != 1)
            {
                overheadAction = parameters.OverheadActionUnroll;
                workloadAction = parameters.WorkloadActionUnroll;
            }
            else
            {
                overheadAction = parameters.OverheadActionNoUnroll;
                workloadAction = parameters.WorkloadActionNoUnroll;
            }
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
            if (measurements.Count > 0 && firstMeasurement == null)
            {
                var measurement = measurements[measurements.Count - 1];
                if (measurement.IterationMode == IterationMode.Workload)
                {
                    firstMeasurement = measurement;
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

        // We do our best to encourage the jit to fully promote methods to tier1, but tiered jit relies on heuristics,
        // and we purposefully don't spend too much time in this stage, so we can't guarantee it.
        // This should succeed for 99%+ of microbenchmarks. For any sufficiently short benchmarks where this fails,
        // the following stages (Pilot and Warmup) will likely take it the rest of the way. Long-running benchmarks may never fully reach tier1.
        private IEnumerator<IterationData> EnumerateIterations()
        {
            ++iterationIndex;
            // If the jit is not tiered, just jit the methods that will be used.
            if (!JitInfo.IsTiered)
            {
                yield return GetDummyIterationData(dummy1Action);
                yield return GetOverheadUnrollIterationData();
                yield return GetDummyIterationData(dummy2Action);
                yield return GetWorkloadUnrollIterationData();
                yield return GetDummyIterationData(dummy3Action);
                didJitUnroll = unrollFactor != 1;

                yield break;
            }

            // For tiered jit, start jitting with single invocations.
            yield return GetDummyIterationData(dummy1Action);
            yield return GetOverheadNoUnrollIterationData();
            yield return GetDummyIterationData(dummy2Action);
            yield return GetWorkloadNoUnrollIterationData();
            yield return GetDummyIterationData(dummy3Action);

            // Don't spend extra time in the jit stage if the invocation time is too long.
            var firstTimeSeconds = TimeInterval.FromNanoseconds(FirstMeasurement.Nanoseconds).ToSeconds();
            if ((firstTimeSeconds / unrollFactor) >= MaxFirstCallSeconds)
            {
                yield break;
            }

            // Wait enough time for jit call counting to begin.
            MaybeSleep(JitInfo.TieredDelay);

            // If the jit is configured for aggressive tiering, run 1 set of iterations per jit tier to fully promote the methods to tier1.
            if (JitInfo.TieredCallCountThreshold == 1)
            {
                // Run the first iterations with the full unroll.
                ++iterationIndex;
                yield return GetOverheadUnrollIterationData();
                yield return GetWorkloadUnrollIterationData();
                didJitUnroll = unrollFactor != 1;

                MaybeSleep(JitInfo.BackgroundCompilationDelay);

                if (JitInfo.IsDPGO)
                {
                    // Run the remaining iterations without unroll.
                    ++iterationIndex;
                    yield return GetOverheadNoUnrollIterationData();
                    yield return GetWorkloadNoUnrollIterationData();
                }

                MaybeSleep(JitInfo.BackgroundCompilationDelay);

                yield break;
            }

            // Otherwise, attempt to run enough invocations to fully promote the methods to tier1, but don't spend too much time in jit stage.
            TimeSpan maxJitTime = TimeSpan.FromSeconds(MaxJitStageSeconds - firstTimeSeconds);
            StartedClock startedClock = parameters.TargetJob.ResolveValue(InfrastructureMode.ClockCharacteristic, parameters.Resolver).Start();

            // Run one iteration with the full unroll.
            ++iterationIndex;
            yield return GetOverheadUnrollIterationData();
            yield return GetWorkloadUnrollIterationData();
            didJitUnroll = unrollFactor != 1;

            // Run the remaining iterations without unroll.
            for (int i = 0; i < JitInfo.TieredCallCountThreshold - unrollFactor; ++i)
            {
                ++iterationIndex;
                yield return GetOverheadNoUnrollIterationData();
                yield return GetWorkloadNoUnrollIterationData();

                if (startedClock.GetElapsed().GetTimeSpan() >= maxJitTime)
                {
                    yield break;
                }
            }

            MaybeSleep(JitInfo.BackgroundCompilationDelay);

            if (JitInfo.IsDPGO)
            {
                for (int i = 0; i < JitInfo.TieredCallCountThreshold - unrollFactor; ++i)
                {
                    ++iterationIndex;
                    yield return GetOverheadNoUnrollIterationData();
                    yield return GetWorkloadNoUnrollIterationData();

                    if (startedClock.GetElapsed().GetTimeSpan() >= maxJitTime)
                    {
                        yield break;
                    }
                }
            }

            MaybeSleep(JitInfo.BackgroundCompilationDelay);
        }

        private IterationData GetOverheadUnrollIterationData()
            => new(IterationMode.Overhead, IterationStage.Jitting, iterationIndex, unrollFactor, unrollFactor, () => { }, () => { }, overheadAction);

        private IterationData GetWorkloadUnrollIterationData()
            => new(IterationMode.Workload, IterationStage.Jitting, iterationIndex, unrollFactor, unrollFactor, parameters.IterationSetupAction, parameters.IterationCleanupAction, workloadAction);

        private IterationData GetOverheadNoUnrollIterationData()
            => new(IterationMode.Overhead, IterationStage.Jitting, iterationIndex, 1, 1, () => { }, () => { }, parameters.OverheadActionNoUnroll);

        private IterationData GetWorkloadNoUnrollIterationData()
            => new(IterationMode.Workload, IterationStage.Jitting, iterationIndex, 1, 1, parameters.IterationSetupAction, parameters.IterationCleanupAction, parameters.WorkloadActionNoUnroll);

        private void MaybeSleep(TimeSpan timeSpan)
        {
            if (timeSpan > TimeSpan.Zero)
            {
                Thread.Sleep(timeSpan);
            }
        }
    }

    internal sealed class EngineSecondJitStage(FrozenEngineParameters parameters) : EngineJitStage(parameters)
    {
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

        internal override bool GetShouldRunIteration(List<Measurement> measurements, out IterationData iterationData)
        {
            // The benchmark method has already been jitted via *NoUnroll, we only need to jit the *Unroll methods here, which aren't tiered.
            iterationData = ++iterationIndex switch
            {
                1 => GetDummyIterationData(dummy1Action),
                2 => new(IterationMode.Overhead, IterationStage.Jitting, iterationIndex, 1, 1, () => { }, () => { }, parameters.OverheadActionUnroll),
                3 => GetDummyIterationData(dummy2Action),
                // IterationSetup/Cleanup are only used for *NoUnroll benchmarks
                4 => new(IterationMode.Workload, IterationStage.Jitting, iterationIndex, 1, 1, () => { }, () => { }, parameters.WorkloadActionUnroll),
                5 => GetDummyIterationData(dummy3Action),
                _ => default
            };
            return iterationIndex <= 5;
        }
    }
}