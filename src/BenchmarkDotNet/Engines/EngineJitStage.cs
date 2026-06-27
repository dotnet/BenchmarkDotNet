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
    // After a tier's single burst fails to tier-up, we nudge one invocation at a time, giving the background worker a
    // short window (~10ms) to pick each nudge up before trying the next — so we stop the instant the tier-up lands
    // instead of overshooting by re-bursting the whole budget. Passed to WaitForTierUp as its busy-wait timeout.
    private static readonly TimeSpan EventDeliveryLag = TimeSpan.FromMilliseconds(10);

    internal bool didStopEarly = false;
    internal Measurement lastMeasurement;

    private readonly IEnumerator<IterationData> enumerator;
    private readonly bool evaluateOverhead;
    private readonly bool skipDelays;
    // Watches the benchmark method(s)' background tier-up via JIT events so we can proceed once the JIT goes quiet after
    // each tier (the whole call tree warmed), instead of waiting a fixed delay. Null when there's nothing to watch or
    // EventSource is disabled, in which case we fall back to the fixed delay.
    private readonly JitListener? listener;
    // True when this stage created the listener and must dispose it; false when a caller (a test) injected one it owns.
    private readonly bool disposeListener;

    internal EngineJitStage(bool evaluateOverhead, EngineParameters parameters, bool skipDelays)
        : this(evaluateOverhead, parameters, JitListener.Create(parameters.WorkloadMethods), disposeListener: true, skipDelays: skipDelays)
    {
    }

    internal EngineJitStage(bool evaluateOverhead, EngineParameters parameters, JitListener? listener, bool disposeListener = false, bool skipDelays = false)
        : base(IterationStage.Jitting, IterationMode.Workload, parameters)
    {
        this.listener = listener;
        this.disposeListener = disposeListener;
        enumerator = EnumerateIterations();
        this.evaluateOverhead = evaluateOverhead;
        this.skipDelays = skipDelays;
    }

    internal override List<Measurement> GetMeasurementList() => new(GetMaxMeasurementCount());

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
        if (disposeListener)
        {
            listener?.Dispose();
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

        bool observeMethod = listener != null;
        if (observeMethod)
        {
            // Before the tier loop, wait until the call-counting delay is inactive so the first burst is counted —
            // or, if tiering is quiet because the method was pre-warmed past tier0, fake it and proceed. The first
            // invoke above already fired the watched method's Pause if it was tier0. See WaitForInitialTieringActive.
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
            // Run ONE full burst of this tier's call budget, gated so it's counted rather than wasted into a
            // deferred window. After it, wait for the background JIT to go QUIET (WaitForQuiescentTierUp): once the
            // worker is idle, this tier's compiles — the watched method(s) AND their untracked callees — have all
            // landed, so the next burst / the following stage won't race them. The per-tier counter persists, so if
            // the burst didn't tier the watched method(s) up we nudge the rest one at a time below rather than
            // re-bursting the whole budget.
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
                // Wait for the background JIT to go quiet (the watched method(s) and their callees settle), then read
                // whether the watched method(s) actually advanced this burst. Once we've stopped observing them the
                // advanced result is ignored, but this still drains untracked-callee tier-ups before the next burst.
                bool advanced = listener.WaitForQuiescentTierUp(tierCount, parameters.Host.CancellationToken);
                if (observeMethod)
                {
                    if (!advanced)
                    {
                        // The burst didn't tier the watched method(s) up. With NO call-counting delay, the burst's whole
                        // budget was counted, so a miss means they were pre-warmed past this tier (or are otherwise
                        // unobservable) — nudging can't help, so stop consulting the listener for them. We don't bail out
                        // entirely because the benchmark may call other (un-pre-warmed) methods via different control
                        // flow (e.g. an InProcess toolchain with arguments/params); the remaining bursts warm those + callees.
                        if (JitInfo.TieredDelay <= TimeSpan.Zero)
                        {
                            observeMethod = false;
                            continue;
                        }

                        // Otherwise the call-counting delay was probably active for the first ~10ms of the burst due to event
                        // delivery lag, so some invocations didn't count and we just need a few more. Re-bursting the whole
                        // budget would overshoot wastefully (up to threshold * call-time), so nudge one invocation at a time,
                        // detecting the tier-up cheaply (WaitForTierUp, no full quiescence settle per nudge), then
                        // settle once at the end so this tier's callees are warm.
                        listener.WaitForTieringActive(parameters.Host.CancellationToken);
                        long nudgeCalls = hasUserInvocationCount ? userInvokeCount : 1;
                        for (long nudged = 0; nudged < JitInfo.TieredCallCountThreshold && !advanced; nudged += nudgeCalls)
                        {
                            ++iterationIndex;
                            yield return GetWorkloadIterationData(nudgeCalls);
                            advanced = listener.WaitForTierUp(tierCount, EventDeliveryLag, parameters.Host.CancellationToken);
                        }
                        // Settle the callees pushed by the nudges (and re-read the tier state race-free); this also
                        // catches a tier-up whose publication arrived just after the last cheap WaitForTierUp window.
                        advanced = listener.WaitForQuiescentTierUp(tierCount, parameters.Host.CancellationToken);
                        if (!advanced)
                        {
                            // Even nudging didn't tier them up — most likely pre-warmed to their final tier before the
                            // stage started (e.g. via InProcess toolchains). Stop consulting the listener (same as the
                            // no-delay case above). We already spent ~2 tiers' worth here, so skip a tier (the extra
                            // ++tierCount on top of the loop's) so we don't overspend the budget.
                            observeMethod = false;
                            ++tierCount;
                            continue;
                        }
                    }

                    if (listener.ReachedFinalTier)
                    {
                        // ReachedFinalTier is the aggregate: every watched method is fully warmed, so we will not
                        // receive any more tier-up JIT events for the method(s) we track. (OSR adds a quirk: a runtime
                        // bug double-instruments an OSR'd callee, which JitInfo.MaxTierPromotions already budgets for.)
                        // Keep bursting to push any untracked callees through their tiers — the quiescence wait above
                        // handles them — but stop consulting the listener for the watched method(s).
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