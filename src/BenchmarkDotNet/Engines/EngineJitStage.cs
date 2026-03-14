using System;
using System.Collections.Generic;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Engines;

// We do our best to encourage the jit to fully promote methods to tier1, but tiered jit relies on heuristics,
// and we purposefully don't spend too much time in this stage, so we can't guarantee it.
// This should succeed for 99%+ of microbenchmarks. For any sufficiently short benchmarks where this fails,
// the following stages (Pilot and Warmup) will likely take it the rest of the way. Long-running benchmarks may never fully reach tier1.
internal sealed class EngineJitStage : EngineStage
{
    // It is not worth spending a long time in jit stage for macro-benchmarks.
    private static readonly TimeInterval MaxTieringTime = TimeInterval.FromSeconds(10);

    // Jit call counting delay is only for when the app starts up. We don't need to wait for every benchmark if multiple benchmarks are ran in-process.
    private static TimeSpan s_tieredDelay = JitInfo.TieredDelay;

    internal bool didStopEarly = false;
    internal Measurement lastMeasurement;

    private readonly IEnumerator<IterationData> enumerator;
    private readonly bool evaluateOverhead;

    internal EngineJitStage(bool evaluateOverhead, EngineParameters parameters) : base(IterationStage.Jitting, IterationMode.Workload, parameters)
    {
        enumerator = EnumerateIterations();
        this.evaluateOverhead = evaluateOverhead;
    }

    internal override List<Measurement> GetMeasurementList() => new(GetMaxMeasurementCount());

    private int GetMaxMeasurementCount()
    {
        int count = JitInfo.IsTiered
            ? JitInfo.MaxTierPromotions * JitInfo.TieredCallCountThreshold + 2
            : 1;
        if (evaluateOverhead)
        {
            count += 1;
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
            yield return GetOverheadIterationData(1);
        }
        yield return GetWorkloadIterationData(1);

        // If the jit is not tiered, we're done.
        if (!JitInfo.IsTiered)
        {
            yield break;
        }

        // Wait enough time for jit call counting to begin.
        Engine.SleepIfPositive(s_tieredDelay);
        // Don't make the next jit stage wait if it's ran in the same process.
        s_tieredDelay = TimeSpan.Zero;

        // Attempt to promote methods to tier1, but don't spend too much time in jit stage.
        StartedClock startedClock = parameters.TargetJob.ResolveValue(InfrastructureMode.ClockCharacteristic, parameters.Resolver)!.Start();

        int remainingTiers = JitInfo.MaxTierPromotions;
        int lastInvokeCount = 1;
        while (remainingTiers > 0)
        {
            --remainingTiers;
            int remainingCalls = JitInfo.TieredCallCountThreshold;
            while (remainingCalls > 0)
            {
                // If we can run one batch of calls within the time limit (based on the last measurement), do that instead of multiple single-invocation iterations.
                var remainingTimeLimit = MaxTieringTime.ToNanoseconds() - startedClock.GetElapsed().GetNanoseconds();
                var lastMeasurementSingleInvocationTime = lastMeasurement.Nanoseconds / lastInvokeCount;
                int allowedCallsWithinTimeLimit = (int) Math.Floor(remainingTimeLimit / lastMeasurementSingleInvocationTime);
                int invokeCount = allowedCallsWithinTimeLimit > 0
                    ? Math.Min(remainingCalls, allowedCallsWithinTimeLimit)
                    : 1;
                lastInvokeCount = invokeCount;

                remainingCalls -= invokeCount;
                ++iterationIndex;
                // The generated __Overhead method is aggressively optimized, so we don't need to run it again.
                yield return GetWorkloadIterationData(invokeCount);

                if ((remainingTiers + remainingCalls) > 0
                    && startedClock.GetElapsed().GetTimeValue() >= MaxTieringTime)
                {
                    didStopEarly = true;
                    yield break;
                }
            }

            Engine.SleepIfPositive(JitInfo.BackgroundCompilationDelay);
        }

        // Empirical evidence shows that the first call after the method is tiered up may take longer,
        // so we run an extra iteration to ensure the next stage gets a stable measurement.
        ++iterationIndex;
        yield return GetWorkloadIterationData(1);
    }

    private IterationData GetOverheadIterationData(long invokeCount)
        => new(IterationMode.Overhead, IterationStage.Jitting, iterationIndex, invokeCount, 1, () => new(), () => new(), parameters.OverheadActionNoUnroll);

    private IterationData GetWorkloadIterationData(long invokeCount)
        => new(IterationMode.Workload, IterationStage.Jitting, iterationIndex, invokeCount, 1, parameters.IterationSetupAction, parameters.IterationCleanupAction, parameters.WorkloadActionNoUnroll);
}