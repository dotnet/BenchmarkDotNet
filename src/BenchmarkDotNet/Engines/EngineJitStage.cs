using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

namespace BenchmarkDotNet.Engines;

// We do our best to encourage the jit to fully promote methods to tier1, but tiered jit relies on heuristics,
// and we purposefully don't spend too much time in this stage, so we can't guarantee it.
// This should succeed for 99%+ of microbenchmarks. For any sufficiently short benchmarks where this fails,
// the following stages (Pilot and Warmup) will likely take it the rest of the way. Long-running benchmarks may never fully reach tier1.
[AggressivelyOptimizeMethods] // Reduce JIT event noise from the jit stage itself.
internal sealed class EngineJitStage : EngineStage
{
    // How long to give the background worker to pick up each nudge before trying the next, so we stop the instant a
    // tier-up lands instead of overshooting.
    private static readonly TimeSpan EventDeliveryLag = TimeSpan.FromMilliseconds(10);

    internal bool didStopEarly = false;
    internal Measurement lastMeasurement;

    private readonly IEnumerator<IterationData> enumerator;
    private readonly bool evaluateOverhead;
    private readonly bool skipDelays;
    private readonly JitListener? listener;

    internal EngineJitStage(bool evaluateOverhead, EngineParameters parameters, bool skipDelays)
        : this(evaluateOverhead, parameters, JitListener.Create(parameters.WorkloadMethods), skipDelays: skipDelays)
    {
    }

    internal EngineJitStage(bool evaluateOverhead, EngineParameters parameters, JitListener? listener, bool skipDelays = false)
        : base(IterationStage.Jitting, IterationMode.Workload, parameters)
    {
        this.listener = listener;
        enumerator = EnumerateIterations();
        this.evaluateOverhead = evaluateOverhead;
        this.skipDelays = skipDelays;
    }

    internal override List<Measurement> GetMeasurementList() => new(GetMaxMeasurementCount());

    public override void Dispose()
    {
        // Do NOT clear fields, they are read in EngineStage.EnumerateStages after the Engine disposes this.
        listener?.Dispose();
        enumerator.Dispose();
        base.Dispose();
    }

    private int GetMaxMeasurementCount()
    {
        int nudgeMultiplier = JitInfo.TieredDelay > TimeSpan.Zero ? 2 : 1;
        int count = JitInfo.IsTiered
            ? JitInfo.MaxTierPromotions * JitInfo.TieredCallCountThreshold * nudgeMultiplier + 2
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
            var measurement = measurements[^1];
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

        bool observeMethod = listener != null;
        if (observeMethod)
        {
            // Wait until the call-counting delay is inactive so the first burst is counted. The invoke above already
            // fired the watched method's Pause if it was tier0. See WaitForInitialTieringActive.
            listener!.WaitForInitialTieringActive(parameters.Host.CancellationToken);
        }
        else if (!skipDelays && JitInfo.TieredDelay > TimeSpan.Zero)
        {
            // Fall back to a fixed wait for the call-counting delay to elapse.
            Thread.Sleep(JitInfo.TieredDelay + TimeSpan.FromMilliseconds(10));
        }

        // Long-running early-exit: if a single invocation already takes ~2/3 of IterationTime, this is a long-running
        // benchmark — bail and let the Pilot/Warmup stages finish tiering. The first invoke can be inflated by JIT or
        // cctors, so confirm with one more iteration before bailing (it could be a one-time cost). #2004
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
        }

        // Promote methods to tier1.
        for (int tierCount = 0; tierCount < JitInfo.MaxTierPromotions; ++tierCount, remainingCalls = JitInfo.TieredCallCountThreshold)
        {
            // Gate the burst so its calls are counted rather than wasted into a deferred window.
            listener?.WaitForTieringActive(parameters.Host.CancellationToken);
            while (remainingCalls > 0)
            {
                // Run the whole tier's call budget in a single iteration unless the user pinned InvocationCount.
                long invokeCount = hasUserInvocationCount ? userInvokeCount : remainingCalls;
                remainingCalls -= invokeCount;
                ++iterationIndex;
                // The generated __Overhead method is aggressively optimized, so we don't need to run it again.
                yield return GetWorkloadIterationData(invokeCount);
            }

            if (listener != null)
            {
                // Settle this tier's compiles — the watched methods AND their untracked callees — so the next burst and
                // the following stage don't race them. Still worth doing for the callees once we've stopped observing,
                // in which case `advanced` is ignored.
                bool advanced = listener.WaitForQuiescentTierUp(tierCount, parameters.Host.CancellationToken);
                if (observeMethod)
                {
                    if (!advanced)
                    {
                        // The burst didn't tier them up. With NO call-counting delay the whole budget was counted, so a
                        // miss means they were pre-warmed past this tier (or are otherwise unobservable) and nudging
                        // can't help — stop consulting the listener. Don't bail out entirely: the benchmark may reach
                        // other, un-pre-warmed methods via different control flow (e.g. InProcess with arguments/params),
                        // which the remaining bursts still warm.
                        if (JitInfo.TieredDelay <= TimeSpan.Zero)
                        {
                            observeMethod = false;
                            continue;
                        }

                        // Otherwise the burst went uncounted because this tier's counting stub was never installed.
                        // Counting is per code version: every tier-up publishes a FRESH stub, whose install is deferred to
                        // the next Resume if the delay happens to be active right then (any brand-new tier0 method's first
                        // call can re-open it; a method's own promotion never does). An installed stub is never revoked by
                        // a later Pause, so a deferred install is the ONLY way a full burst fails to count — the calls
                        // simply weren't counted, and a few more will do.
                        //
                        // Our gate didn't prevent it because events lag ~10ms: the Resume we gated on was real, but a Pause
                        // opened after it hadn't reached us, leaving the gate stale-Set. So re-gate rather than burst blind
                        // — that Pause has likely landed by now, and the Resume flushes the pending stub.
                        listener.WaitForTieringActive(parameters.Host.CancellationToken);
                        // Nudge one invocation at a time instead of re-bursting the whole budget (which would overshoot by
                        // up to threshold * call-time), detecting the tier-up without a full quiescence settle per nudge.
                        long nudgeCalls = hasUserInvocationCount ? userInvokeCount : 1;
                        for (long nudged = 0; nudged < JitInfo.TieredCallCountThreshold && !advanced; nudged += nudgeCalls)
                        {
                            ++iterationIndex;
                            yield return GetWorkloadIterationData(nudgeCalls);
                            advanced = listener.WaitForTierUp(tierCount, EventDeliveryLag, parameters.Host.CancellationToken);
                        }
                        // Settle the callees the nudges pushed, and catch a tier-up published just after the last nudge's window.
                        advanced = listener.WaitForQuiescentTierUp(tierCount, parameters.Host.CancellationToken);
                        if (!advanced)
                        {
                            // Not even nudging tiered them up — most likely pre-warmed to their final tier before the
                            // stage started. Stop consulting the listener, and skip a tier (the extra ++tierCount) since
                            // we already spent ~2 tiers' budget here.
                            observeMethod = false;
                            ++tierCount;
                            continue;
                        }
                    }

                    if (listener.ReachedFinalTier)
                    {
                        // Every watched method is warmed, so no more tier-up events are coming for them. Keep bursting
                        // anyway to push untracked callees through their tiers (an OSR'd one can lag — a runtime bug
                        // double-instruments it, which JitInfo.MaxTierPromotions budgets for), just stop consulting.
                        observeMethod = false;
                    }
                }
            }
            else if (!skipDelays)
            {
                // No listener (nothing to watch, or EventSource unavailable), fall back to the fixed delay.
                Engine.SleepIfPositive(JitInfo.BackgroundCompilationDelay);
            }
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