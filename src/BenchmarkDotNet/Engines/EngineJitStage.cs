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
    // After a tier's single burst fails to tier-up, we nudge one invocation at a time and wait out the async
    // event-delivery lag (~10ms) after each before nudging again — so we stop the instant the next tier's
    // MethodLoadVerbose publication confirms the tier-up instead of overshooting by re-bursting the whole budget.
    private static readonly TimeSpan EventDeliveryLag = TimeSpan.FromMilliseconds(10);

    // How long to wait for the JIT to be quiet (not compiling any tiered methods in the background).
    private static readonly TimeSpan JitQuiescenceWindow = TimeSpan.FromMilliseconds(50);

    // How long to wait for an observed-busy background JIT batch to drain before assuming its BackgroundJitStop was
    // dropped by EventPipe and proceeding. Generous — it only bites on a dropped event; a real drain completes sooner.
    private static readonly TimeSpan BackgroundJitDrainTimeout = TimeSpan.FromSeconds(10);

    internal bool didStopEarly = false;
    internal Measurement lastMeasurement;

    private readonly IEnumerator<IterationData> enumerator;
    private readonly bool evaluateOverhead;
    // Watches for the method's background tier-up via JIT events so we can proceed as soon as each tier is published.
    // Null when watching is disabled or EventSource is disabled, in which case we fall back to the fixed delay.
    private readonly JitListener? listener;
    // True when this stage created the listener and must dispose it; false when a caller (a test) injected one it owns.
    private readonly bool disposeListener;

    internal EngineJitStage(bool evaluateOverhead, EngineParameters parameters)
        : this(evaluateOverhead, parameters, JitListener.Create(parameters.WorkloadMethod, parameters.EnableJitListener), disposeListener: true)
    {
    }

    internal EngineJitStage(bool evaluateOverhead, EngineParameters parameters, JitListener? listener, bool disposeListener = false)
        : base(IterationStage.Jitting, IterationMode.Workload, parameters)
    {
        this.listener = listener;
        this.disposeListener = disposeListener;
        enumerator = EnumerateIterations();
        this.evaluateOverhead = evaluateOverhead;
    }

    internal override List<Measurement> GetMeasurementList() => new(GetMaxMeasurementCount());

    private int GetMaxMeasurementCount()
    {
        int count = JitInfo.IsTiered
            // Per tier: one full burst plus up to a threshold of single-call nudges (×2 covers the worst case of a
            // user-pinned InvocationCount of 1, where the burst is also split into single-call iterations).
            ? JitInfo.MaxTierPromotions * JitInfo.TieredCallCountThreshold * 2 + 2
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
        else if (JitInfo.TieredDelay > TimeSpan.Zero)
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
        for (int remainingTiers = JitInfo.MaxTierPromotions; remainingTiers > 0; --remainingTiers, remainingCalls = JitInfo.TieredCallCountThreshold)
        {
            // Run ONE full burst of this tier's call budget, gated so it's counted rather than wasted into a
            // deferred window. The next tier's publication (a non-tier0 MethodLoadVerbose) is the trustworthy
            // "the count reached the threshold and the next tier compiled" signal — the persistent per-tier counter
            // means calls accumulate, so if the burst doesn't tier up we nudge the rest one at a time below rather
            // than re-bursting the whole budget.
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

            if (observeMethod)
            {
                // Background compilation can take an indeterminate amount of time. Ideally we would wait for the MethodJittingStarted event,
                // but it doesn't carry tier information, so we can't skip it for the async tier0 events (if we try there is a race condition).
                // The only thing we can do safely is wait for the compilation to complete with a sensible timeout via the MethodLoadVerbose event that carries the tier info.
                // If the publication doesn't arrive in the window, the tier-up may still be compiling, so wait for the
                // JIT to go quiet and re-check (TryQuiescentPublication) before spending nudges.
                bool tieredUp = listener!.WaitForPublication(JitInfo.BackgroundCompilationDelay, parameters.Host.CancellationToken)
                    || TryQuiescentPublication(parameters.Host.CancellationToken);
                if (!tieredUp)
                {
                    // Unlikely, but technically possible. The call-counting delay could be active,
                    // but we don't receive the event for it for 10ms, so the initial burst ran some invocations
                    // that didn't count. In that case it's most likely that most of the invocations did count, so
                    // we only need a few more to nudge it over the threshold. Re-bursting the whole budget would overshoot
                    // wastefully (up to threshold * call-time), so nudge one invocation at a time, waiting out the
                    // async event-delivery lag after each so we stop the instant the tier-up is confirmed. Gate
                    // first so the stub is live (handles a fully-deferred burst).
                    // - or -
                    // The method could have been pre-warmed to tier1 before the stage started (e.g. via InProcess toolchains),
                    // which would also hit this case. In that case we will waste time with unnecessary calls,
                    // but it's impossible for us to detect that scenario with the available JIT APIs.
                    listener.WaitForTieringActive(parameters.Host.CancellationToken);
                    long nudgeCalls = hasUserInvocationCount ? userInvokeCount : 1;
                    for (long nudged = 0; nudged < JitInfo.TieredCallCountThreshold && !tieredUp; nudged += nudgeCalls)
                    {
                        ++iterationIndex;
                        yield return GetWorkloadIterationData(nudgeCalls);
                        tieredUp = listener.WaitForPublication(EventDeliveryLag, parameters.Host.CancellationToken);
                    }
                    // If a whole threshold of nudges went without a confirmed tier-up, it is most likely the case that the method
                    // was already pre-warmed to its final tier. Wait it out once more (and re-check after quiescence) just in case.
                    if (!tieredUp)
                    {
                        tieredUp = listener.WaitForPublication(JitInfo.BackgroundCompilationDelay, parameters.Host.CancellationToken)
                            || TryQuiescentPublication(parameters.Host.CancellationToken);
                    }
                }

                if (!tieredUp)
                {
                    // The method didn't tier up — most likely it was pre-warmed past where we can still see its events.
                    // Stop consulting the listener for the method's tier-ups; the remaining budget warms untracked
                    // callees on the quiescence path below instead. We already invoked and waited ~2 stages' worth, so
                    // subtract a tier to not waste an extra one. We don't bail out entirely because the benchmark may
                    // call other (un-pre-warmed) methods via different control flow (e.g. an InProcess toolchain with arguments/params).
                    observeMethod = false;
                    --remainingTiers;
                    continue;
                }

                if (listener.ReachedFinalTier)
                {
                    // If the method has reached its final tier we will not receive any more JIT events for it.
                    // In case OSR is enabled and the method calls another method that is OSR'd, a runtime bug causes that other method to duplicate a tier (JitInfo.MaxTierPromotions already accounts for it).
                    // Or the method could have been pre-warmed before the stage started, but the benchmark case uses a different control flow that calls different methods that were not pre-warmed.
                    // In either case, the listener only tracks the benchmark method, and unknown callees can't be watched, so stop consulting it for tier-ups.
                    // The remaining budget iterations instead keep bursting to push any such callee through its tiers, waiting for the background JIT queue to go quiet (below)
                    // instead of sleeping the full fixed delay each time.
                    observeMethod = false;
                }
            }
            else if (listener != null)
            {
                // We're no longer driving tier-ups of the benchmark method (it reached its final tier, or its tier-up
                // couldn't be confirmed — e.g. it was pre-warmed), but the burst may still push untracked callees
                // through their tiers. So wait for the background JIT queue to go quiet instead of sleeping the fixed
                // delay: while the worker is (or becomes) busy within the window, drain that batch and re-check; once
                // it stays idle for a whole window, this tier's callee work is done and we loop to burst the next.
                // Tracking busy/idle STATE means a batch already in flight is seen, and a callee that enqueues just
                // after the worker momentarily went idle is still caught within the window.
                while (listener.WaitForBackgroundJitBusy(JitQuiescenceWindow, parameters.Host.CancellationToken))
                {
                    listener.WaitForBackgroundJitIdle(BackgroundJitDrainTimeout, parameters.Host.CancellationToken);
                }
            }
            else
            {
                // No listener at all (no tiered JIT, or EventSource unavailable): fall back to the fixed delay.
                Engine.SleepIfPositive(JitInfo.BackgroundCompilationDelay);
            }
        }

        // Empirical evidence shows that the first call after the method is tiered up may take longer,
        // so we run an extra iteration to ensure the next stage gets a stable measurement.
        ++iterationIndex;
        yield return GetWorkloadIterationData(userInvokeCount);
    }

    // After a publication-wait times out, the tier-up may simply still be compiling in the background.
    // Wait for the JIT worker to go quiet, then re-check for the publication.
    private bool TryQuiescentPublication(CancellationToken cancellationToken)
    {
        if (listener!.WaitForBackgroundJitBusy(JitQuiescenceWindow, cancellationToken))
        {
            listener.WaitForBackgroundJitIdle(BackgroundJitDrainTimeout, cancellationToken);
        }
        return listener.WaitForPublication(TimeSpan.Zero, cancellationToken);
    }

    private IterationData GetOverheadIterationData(long invokeCount)
        => new(IterationMode.Overhead, IterationStage.Jitting, iterationIndex, invokeCount, 1, () => new(), () => new(), parameters.OverheadActionNoUnroll);

    private IterationData GetWorkloadIterationData(long invokeCount)
        => new(IterationMode.Workload, IterationStage.Jitting, iterationIndex, invokeCount, 1, parameters.IterationSetupAction, parameters.IterationCleanupAction, parameters.WorkloadActionNoUnroll);
}