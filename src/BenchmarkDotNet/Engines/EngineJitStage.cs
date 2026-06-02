using System.Reflection;
using System.Runtime.CompilerServices;
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
    // After a tier's single burst fails to tier-up, we nudge one invocation at a time and wait out the async
    // event-delivery lag (~10ms) after each before nudging again — so we stop the instant the next tier's
    // MethodLoadVerbose publication confirms the tier-up instead of overshooting by re-bursting the whole budget.
    private static readonly TimeSpan EventDeliveryLag = TimeSpan.FromMilliseconds(10);

    internal bool didStopEarly = false;
    internal Measurement lastMeasurement;

    private readonly IEnumerator<IterationData> enumerator;
    private readonly bool evaluateOverhead;
    // Watches for the method's background tier-up via JIT events so we can proceed as soon as each tier is published.
    // Null when watching is disabled or EventSource is disabled, in which case we fall back to the fixed delay.
    private readonly JitListener? listener;

    internal EngineJitStage(bool evaluateOverhead, EngineParameters parameters)
        : this(evaluateOverhead, parameters, JitListener.Create(parameters.WorkloadMethod, parameters.EnableJitListener))
    {
    }

    internal EngineJitStage(bool evaluateOverhead, EngineParameters parameters, JitListener? listener)
        : base(IterationStage.Jitting, IterationMode.Workload, parameters)
    {
        this.listener = listener;
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
        listener?.Dispose();
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

        bool useListener = listener != null;
        if (JitInfo.TieredDelay > TimeSpan.Zero)
        {
            if (useListener)
            {
                bool waitForTieringActive = true;
                if (!listener!.WaitForTieringActivePrimed(JitInfo.TieredDelay + TimeSpan.FromMilliseconds(50), parameters.Host.CancellationToken))
                {
                    // If we observed no tier0 JIT (of any method) and no TieredCompilationResume/Pause event within the
                    // timeout, tiering is quiet in the process — which likely means the watched method was pre-warmed to at
                    // least tier0 and the listener was possibly created after its tiered compilation was resumed. In that
                    // case, we force a tier0 JIT which forces a TieredCompilationPause event, which guarantees a followup
                    // TieredCompilationResume that we can wait on deterministically.
                    if (!TryForceTier0Jit())
                    {
                        // We couldn't establish that the call-counting delay is (or will be) active, and can't force one.
                        // Stop trusting the listener's tiering gate for the rest of the stage and fall back to the fixed delay.
                        waitForTieringActive = false;
                        useListener = false;
                        Thread.Sleep(JitInfo.TieredDelay);
                    }
                }
                if (waitForTieringActive)
                {
                    listener!.WaitForTieringActive(parameters.Host.CancellationToken);
                }
            }
            else
            {
                Thread.Sleep(JitInfo.TieredDelay);
            }
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
        for (int remainingTiers = JitInfo.MaxTierPromotions; remainingTiers > 0; --remainingTiers)
        {
            // Run ONE full burst of this tier's call budget, gated so it's counted rather than wasted into a
            // deferred window. The next tier's publication (a non-tier0 MethodLoadVerbose) is the trustworthy
            // "the count reached the threshold and the next tier compiled" signal — the persistent per-tier counter
            // means calls accumulate, so if the burst doesn't tier up we nudge the rest one at a time below rather
            // than re-bursting the whole budget.
            if (useListener)
            {
                listener!.WaitForTieringActive(parameters.Host.CancellationToken);
            }
            while (remainingCalls > 0)
            {
                // Run the whole tier's call budget in a single iteration unless the user pinned InvocationCount.
                long invokeCount = hasUserInvocationCount ? userInvokeCount : remainingCalls;
                remainingCalls -= invokeCount;
                ++iterationIndex;
                // The generated __Overhead method is aggressively optimized, so we don't need to run it again.
                yield return GetWorkloadIterationData(invokeCount);
            }

            if (useListener)
            {
                // Background compilation can take an indeterminate amount of time. Ideally we would wait for the MethodJittingStarted event,
                // but it doesn't carry tier information, so we can't skip it for the async tier0 events (if we try there is a race condition).
                // The only thing we can do safely is wait for the compilation to complete with a sensible timeout via the MethodLoadVerbose event that carries the tier info.
                bool tieredUp = listener!.WaitForPublication(JitInfo.BackgroundCompilationDelay, parameters.Host.CancellationToken);
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
                    listener!.WaitForTieringActive(parameters.Host.CancellationToken);
                    long nudgeCalls = hasUserInvocationCount ? userInvokeCount : 1;
                    for (long nudged = 0; nudged < JitInfo.TieredCallCountThreshold && !tieredUp; nudged += nudgeCalls)
                    {
                        ++iterationIndex;
                        yield return GetWorkloadIterationData(nudgeCalls);
                        tieredUp = listener!.WaitForPublication(EventDeliveryLag, parameters.Host.CancellationToken);
                    }
                    // If a whole threshold of nudges went without a confirmed tier-up, it is most likely the case that the
                    // method was already pre-warmed to tier1. Wait it out once more just in case, then fallback to the fixed delay.
                    // We don't bail out here because it's possible the benchmark will call other methods via different control flow
                    // (e.g. InProcess toolchain with arguments/params).
                    if (!tieredUp && !listener!.WaitForPublication(JitInfo.BackgroundCompilationDelay, parameters.Host.CancellationToken))
                    {
                        useListener = false;
                        // We already invoked and waited 2 stages worth, subtract 1 tier and continue the loop here to not waste an extra stage of unnecessary invocations.
                        remainingCalls = JitInfo.TieredCallCountThreshold;
                        --remainingTiers;
                        continue;
                    }
                }

                if (listener!.ReachedFinalTier)
                {
                    // If the method has reached its final tier we will not receive any more JIT events for it.
                    // In case OSR is enabled and the method calls another method that is OSR'd, a runtime bug causes that other method to duplicate a tier (JitInfo.MaxTierPromotions already accounts for it).
                    // Or the method could have been pre-warmed before the stage started, but the benchmark case uses a different control flow that calls different methods that were not pre-warmed.
                    // In either case, the listener only tracks the benchmark method, and unknown callees can't be watched,
                    // so stop consulting it and let the loop run the remaining calculated promotion iterations on the fixed delay.
                    useListener = false;
                    listener!.WaitForTieringActive(parameters.Host.CancellationToken);
                }
            }
            else
            {
                Engine.SleepIfPositive(JitInfo.BackgroundCompilationDelay);
            }
            remainingCalls = JitInfo.TieredCallCountThreshold;
        }

        // Empirical evidence shows that the first call after the method is tiered up may take longer,
        // so we run an extra iteration to ensure the next stage gets a stable measurement.
        ++iterationIndex;
        yield return GetWorkloadIterationData(userInvokeCount);
    }

    // Throwaway type used only as a generic argument: nesting it (Wrapper<Wrapper<int>>) makes a never-before-seen
    // closed type on demand, so each ForceTier0JitTarget instantiation is a distinct MethodDesc that JITs fresh.
    private struct Wrapper<T> { }

    // A real (non-trivial, non-inlined) generic method so each value-type instantiation gets its own tier0 JIT — and
    // thus runs HandleCallCountingForFirstCall, starting a call-counting delay we can wait on.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ForceTier0JitTarget<T>(long x) => x * default(T)!.GetHashCode();

    private static readonly MethodInfo ForceTargetMethod =
        typeof(EngineJitStage).GetMethod(nameof(ForceTier0JitTarget), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static Type s_nextForceType = typeof(int);
    private static readonly object[] BoxedZero = [0L];
    // This (engine) assembly: a forced JIT of ForceTier0JitTarget below would never start a call-counting delay when
    // optimizations are disabled here, so we skip forcing in that case.
    private static readonly bool OptimizationsDisabled = JitListener.AreOptimizationsDisabledFor(typeof(EngineJitStage));

    // Manufacture a tier0 JIT to start a call-counting delay, for the rare already-tiered method whose own delay
    // elapsed before we were listening. Returns false if it couldn't run — the caller then falls back to a fixed sleep
    // instead of waiting for a Resume that would never come.
    private bool TryForceTier0Jit()
    {
        // In a DisableOptimizations build this assembly's methods are never tier-eligible, so forcing a tier0 JIT here
        // is a silent no-op that starts no delay. Bail rather than commit the caller to an unbounded wait for a Resume.
        if (OptimizationsDisabled)
        {
            return false;
        }
        try
        {
            Type forceType;
            while (true)
            {
                forceType = Volatile.Read(ref s_nextForceType);
                if (Interlocked.CompareExchange(ref s_nextForceType, typeof(Wrapper<>).MakeGenericType(forceType), forceType) == forceType)
                {
                    break;
                }
            }
            ForceTargetMethod.MakeGenericMethod(forceType).Invoke(null, BoxedZero);
            return true;
        }
        catch (Exception e)
        {
            parameters.Host.SendError(e.ToString());
            return false;
        }
    }

    private IterationData GetOverheadIterationData(long invokeCount)
        => new(IterationMode.Overhead, IterationStage.Jitting, iterationIndex, invokeCount, 1, () => new(), () => new(), parameters.OverheadActionNoUnroll);

    private IterationData GetWorkloadIterationData(long invokeCount)
        => new(IterationMode.Workload, IterationStage.Jitting, iterationIndex, invokeCount, 1, parameters.IterationSetupAction, parameters.IterationCleanupAction, parameters.WorkloadActionNoUnroll);
}