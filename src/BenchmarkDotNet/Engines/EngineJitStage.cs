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
        // If the user pinned InvocationCount (e.g. via [IterationSetup]/[IterationCleanup] which implies RunOncePerIteration),
        // honor it so IterationSetup/Cleanup runs around each invocation. #3102
        bool hasUserInvocationCount = parameters.TargetJob.HasValue(RunMode.InvocationCountCharacteristic);
        long userInvokeCount = parameters.TargetJob.ResolveValue(RunMode.InvocationCountCharacteristic, parameters.Resolver, 1);

        ++iterationIndex;
        if (evaluateOverhead)
        {
            yield return GetOverheadIterationData(1);
        }
        yield return GetWorkloadIterationData(userInvokeCount);

        JitTieringMode jitTieringMode = parameters.TargetJob.ResolveValue(RunMode.JitTieringModeCharacteristic, parameters.Resolver);
        // If the jit is not tiered, or the user wants to skip, we're done.
        if (!JitInfo.IsTiered || jitTieringMode == JitTieringMode.Skip)
        {
            yield break;
        }

        // Wait enough time for jit call counting to begin.
        Engine.SleepIfPositive(s_tieredDelay);
        // Don't make the next jit stage wait if it's ran in the same process.
        s_tieredDelay = TimeSpan.Zero;

        // If the first iteration suggests a long-running benchmark (a single invocation already
        // takes ~2/3 of IterationTime or more), run one confirmation iteration and bail out if
        // it agrees. Same cutoff value that pilot stage uses.
        // We do not bail out immediately if the first iteration is long-running because it could
        // be due to cctors or other lazy initialization that won't be hit in steady-state. #2004
        // JitTieringMode.Force opts out of this heuristic and always promotes through every tier.
        TimeInterval iterationTime = parameters.TargetJob.ResolveValue(RunMode.IterationTimeCharacteristic, parameters.Resolver);
        long remainingCalls = JitInfo.TieredCallCountThreshold;
        if (jitTieringMode == JitTieringMode.Auto
            && iterationTime.Nanoseconds / (lastMeasurement.Nanoseconds / (double)userInvokeCount) < 1.5)
        {
            ++iterationIndex;
            yield return GetWorkloadIterationData(userInvokeCount);
            if (iterationTime.Nanoseconds / (lastMeasurement.Nanoseconds / (double)userInvokeCount) < 1.5)
            {
                didStopEarly = true;
                yield break;
            }
            remainingCalls -= userInvokeCount;
        }

        // Promote methods to tier1.
        for (int remainingTiers = JitInfo.MaxTierPromotions; remainingTiers > 0; --remainingTiers)
        {
            while (remainingCalls > 0)
            {
                // Run the whole tier's call budget in a single iteration unless the user pinned InvocationCount.
                long invokeCount = hasUserInvocationCount ? userInvokeCount : remainingCalls;
                remainingCalls -= invokeCount;
                ++iterationIndex;
                // The generated __Overhead method is aggressively optimized, so we don't need to run it again.
                yield return GetWorkloadIterationData(invokeCount);
            }

            Engine.SleepIfPositive(JitInfo.BackgroundCompilationDelay);
            remainingCalls = JitInfo.TieredCallCountThreshold;
        }

        // Empirical evidence shows that the first call after the method is tiered up may take longer,
        // so we run an extra iteration to ensure the next stage gets a stable measurement.
        ++iterationIndex;
        yield return GetWorkloadIterationData(userInvokeCount);
    }

    private IterationData GetOverheadIterationData(long invokeCount)
        => new(IterationMode.Overhead, IterationStage.Jitting, iterationIndex, invokeCount, 1, () => new(), () => new(), parameters.OverheadActionNoUnroll);

    private IterationData GetWorkloadIterationData(long invokeCount)
        => new(IterationMode.Workload, IterationStage.Jitting, iterationIndex, invokeCount, 1, parameters.IterationSetupAction, parameters.IterationCleanupAction, parameters.WorkloadActionNoUnroll);
}