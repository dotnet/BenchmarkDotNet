using System.Diagnostics.Tracing;
using System.Reflection;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Engines;

// Observes background JIT tier-up of a single (benchmark) method by listening to the runtime's JIT events
// in-process, so the jit stage can proceed as soon as the next tier is actually reached instead of waiting a
// fixed delay. The runtime only announces transitions (there is no API to poll a method's current tier), so we
// must be listening while they happen.
//
// The events it watches, and their roles:
//   * MethodLoadVerbose (per-method, JIT keyword) reports each tier publication and carries the tier. A burst that
//     reaches the call-count threshold triggers the next tier's compile, which publishes a (non-tier0) load — so the
//     first such publication after a burst is the AUTHORITATIVE "the burst tiered up" signal, and the tier it carries
//     tells us when the method reached its final tier. The stage keeps invoking until it sees one. (WaitForPublication /
//     ReachedFinalTier.) We deliberately do NOT use MethodJittingStarted (compile-began): it carries no tier, so the
//     tier0 compile's start is indistinguishable from a tier-up's and would race the tier0 publish that filters it.
//   * TieredCompilationPause/Resume (the call-counting delay bracket, Compilation keyword) gate the bursts: a burst
//     issued while the delay is active isn't counted (the counting stub is deferred), so the stage waits until the
//     delay is observed inactive — a Resume, when the stubs are installed — before bursting (WaitForTieringActive). Up
//     front (WaitForInitialTieringActive) it waits for any method's tier0 JIT or a Pause/Resume to confirm a Resume is
//     coming; if none arrives the method was pre-warmed and its stub is already installed, so it fakes the inactive
//     state and proceeds. These only avoid wasting bursts — correctness comes from the publication.
//
// This is intentionally a per-stage listener: enabling the Jit keyword emits an event for every method jitted
// process-wide, which we must NOT pay during the measurement stages. It is created at the start of the jit stage
// and disposed at the end.
//
// Create returns null (and the caller falls back to the fixed delay) when the runtime has no tiered JIT, or when
// EventSource is unavailable — it can be disabled via the System.Diagnostics.Tracing.EventSource.IsSupported feature
// switch. It otherwise watches the method regardless of whether it looks tier-eligible: a method that can't tier just
// publishes its single final tier (see the tier constants below), which the stage observes and treats as "done".
internal sealed class JitListener : EventListener
{
    private const string RuntimeEventSourceName = "Microsoft-Windows-DotNETRuntime";
    private const EventKeywords JitKeyword = (EventKeywords)0x10;
    // The "Compilation" keyword carries the TieredCompilation/Pause|Resume events that bracket the runtime's
    // call-counting delay. Low volume (a handful per delay cycle), so enabling it adds no meaningful cost.
    private const EventKeywords CompilationKeyword = (EventKeywords)0x1000000000;
    private const string TieredCompilationResumeEvent = "TieredCompilationResume";
    private const string TieredCompilationPauseEvent = "TieredCompilationPause";
    // The background tiering worker brackets each batch with these: Start when it begins draining its queue, Stop when
    // it finishes (Stop's PendingMethodCount payload is how many remain — 0 = drained). Unlike per-method JIT events
    // these fire ONLY for actual tiered background work, so a Start is a clean "an untracked callee is tiering up"
    // signal. Used to wait out such callees once the watched method itself is fully warmed.
    private const string TieredCompilationBackgroundJitStartEvent = "TieredCompilationBackgroundJitStart";
    private const string TieredCompilationBackgroundJitStopEvent = "TieredCompilationBackgroundJitStop";
    // Event-name prefix (the runtime appends a version suffix, e.g. MethodLoadVerbose_V2).
    private const string MethodLoadVerbosePrefix = "MethodLoadVerbose";

    // Optimization tier is packed into MethodFlags bits [7..9]: (MethodFlags >> 7) & 0x7.
    // The initial tier0 quick compile is QuickJitted = 3; the intermediate instrumented (PGO) publication reports
    // another value and just counts as "a recompilation happened". A method is fully warmed once it reaches one of
    // the runtime's FINAL tiers — those from which no further tier-up is coming:
    //   * OptimizedTier1 = 4 — the usual steady state for a tier-eligible method.
    //   * Optimized = 2 (NativeCodeVersion::OptimizationTierOptimized) — a method compiled straight to optimized code
    //     without a tier1 promotion: AggressiveOptimization, or a method with a loop when TC_QuickJitForLoops is off.
    //   * MinOptJitted = 1 — a method that never tiers at all: NoOptimization, or any method in an
    //     optimization-disabled assembly. This is its first and only compile.
    // Since Create now watches every method (not just ones that look tier-eligible), a non-tiering method publishes
    // exactly one of MinOptJitted/Optimized and we recognize it as final immediately, rather than predicting it from
    // attributes. OptimizedTier1OSR = 5 is special: an on-stack-replacement of a still-running body with a hot loop.
    // It fires off the loop's back-edge counter, NOT off the call-count threshold, so unlike every other tier it is
    // never the method's active entry-point code version and is never call-counted — it's orthogonal to the
    // call-count tier ladder the stage drives, and a watched method that OSRs in both its tier0 and instrumented
    // bodies emits two of them on the way to its final tier. We therefore ignore OSR publications for our method (see
    // HandleMethodLoad) so they don't consume the stage's per-tier publication budget and stall it short of the final tier.
    private const int OptimizationTierShift = 7;
    private const int OptimizationTierMask = 0x7;
    private const int MinOptJitted = 1;
    private const int Optimized = 2;
    private const int QuickJittedTier0 = 3;
    private const int OptimizedTier1 = 4;
    private const int OptimizedTier1OSR = 5;

    private readonly int metadataToken;
    private readonly string methodName;
    private readonly ManualResetEventSlim publicationSignal = new(false);
    private readonly ManualResetEventSlim tieringActiveSignal = new(false);
    private readonly ManualResetEventSlim tieringActivePrimedSignal = new(false);
    // Reflect the background tiering worker's STATE (used only after the watched method reaches its final tier, to
    // wait out untracked callees). The runtime brackets each batch with BackgroundJitStart..Stop and the worker is
    // single-threaded, so these stay complementary: busy while a batch is running, idle otherwise. Tracking state
    // rather than a start edge means a batch already in flight when the stage looks is observed — there is no manual
    // reset that could wipe a "started" we hadn't seen yet.
    private readonly ManualResetEventSlim backgroundJitBusySignal = new(false);
    private readonly ManualResetEventSlim backgroundJitIdleSignal = new(true);

    private volatile bool reachedFinalTier;
    private volatile bool canObserve;

    // Cached payload indices (field order is stable within a process for a given event version).
    private int loadTokenIndex = -1;
    private int loadFlagsIndex = -1;
    private int loadNameIndex = -1;
    private int backgroundJitStopPendingIndex = -1;

    private JitListener(MethodInfo method)
    {
        // NOTE: the base EventListener ctor calls OnEventSourceCreated before these fields are set,
        // but that callback only enables events / probes canObserve and never reads them.
        metadataToken = method.MetadataToken;
        methodName = method.Name;
    }

    internal static JitListener? Create(MethodInfo method, bool enabled = true)
    {
        if (!enabled || !JitInfo.IsTiered)
        {
            return null;
        }
        var listener = new JitListener(method);
        if (!listener.canObserve)
        {
            listener.Dispose();
            return null;
        }
        return listener;
    }

    internal bool ReachedFinalTier => reachedFinalTier;

    // Waits until the call-counting delay is observed inactive (a TieredCompilationResume), so the stage's first burst
    // will be counted. It first waits up to a timeout for any sign the tiering machinery is active — a tier0 (QuickJitted)
    // publication for ANY method, or a TieredCompilation Pause/Resume — which guarantees a Resume is coming to gate on.
    // (The stage calls this AFTER its first invoke, so a freshly-tier0 watched method has already fired its Pause.) If
    // nothing arrives within the timeout, tiering is quiet: the watched method was pre-warmed past tier0, its stub is
    // already installed, and no delay is coming on its own — so we fake the active state and proceed. The lock + IsSet
    // re-check makes that fake atomic against a real event landing right at the timeout boundary (the event handlers
    // take the same lock), so we never overwrite one; and we wait OUTSIDE the lock so the handlers never block on us.
    internal void WaitForInitialTieringActive(CancellationToken cancellationToken)
    {
        // No call-counting delay (e.g. AggressiveTiering) — counting is armed immediately, nothing to gate on.
        if (JitInfo.TieredDelay <= TimeSpan.Zero)
        {
            return;
        }
        if (!tieringActivePrimedSignal.Wait(JitInfo.TieredDelay + TimeSpan.FromMilliseconds(50), cancellationToken))
        {
            lock (tieringActivePrimedSignal)
            {
                if (!tieringActivePrimedSignal.IsSet)
                {
                    tieringActivePrimedSignal.Set();
                    tieringActiveSignal.Set();
                }
            }
        }
        WaitForTieringActive(cancellationToken);
    }

    // Waits until the call-counting delay is inactive (a TieredCompilationResume was observed). Re-gates each burst in
    // the tier loop after WaitForInitialTieringActive established the delay was inactive up front.
    internal void WaitForTieringActive(CancellationToken cancellationToken)
    {
        // No call-counting delay (e.g. AggressiveTiering) — counting is armed immediately, nothing to gate on.
        if (JitInfo.TieredDelay > TimeSpan.Zero)
        {
            tieringActiveSignal.Wait(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }

    // Waits for a new tier publication (a non-tier0 MethodLoadVerbose for the method) — i.e. the latest burst drove
    // the method to its next tier and the runtime published it. True if one arrived before the timeout; false otherwise.
    internal bool WaitForPublication(TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (!publicationSignal.Wait(timeout, cancellationToken))
        {
            return false;
        }
        // Reset for the next tier. We can't use AutoResetEvent because it doesn't support CancellationToken.
        publicationSignal.Reset();
        return true;
    }

    // Waits (up to the timeout) for the background tiering worker to be running a batch — either one already in flight
    // or one that starts within the window. True if it is/becomes busy; false if it stays idle the whole timeout,
    // meaning no background tiering is underway (quiet).
    internal bool WaitForBackgroundJitBusy(TimeSpan timeout, CancellationToken cancellationToken)
        => backgroundJitBusySignal.Wait(timeout, cancellationToken);

    // Waits for the background tiering worker to go idle (its queue drained — a BackgroundJitStop with
    // PendingMethodCount == 0). No timeout: the caller only waits after observing the worker busy, and a running
    // batch always finishes, so idle is guaranteed to arrive (the host's token still bounds it).
    internal void WaitForBackgroundJitIdle(CancellationToken cancellationToken)
        => backgroundJitIdleSignal.Wait(cancellationToken);

    protected override void OnEventSourceCreated(EventSource source)
    {
        if (source.Name == RuntimeEventSourceName)
        {
            EnableEvents(source, EventLevel.Verbose, JitKeyword | CompilationKeyword);
            // IsEnabled is true only when EventSource is supported AND the enable actually took effect
            // at the level/keyword we need.
            canObserve = source.IsEnabled(EventLevel.Verbose, JitKeyword | CompilationKeyword);
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs e)
    {
        if (!canObserve)
            return;
        string? name = e.EventName;
        if (name is null)
            return;

        // The runtime brackets the call-counting delay with these: Pause when a new tier0 method's first call
        // (re)starts the delay, Resume when it elapses and the whole pending list of counting stubs is installed.
        // tieringActiveSignal is the flip-flop the burst gate waits on (Set on Resume = delay inactive = stubs live,
        // Reset on Pause); tieringActivePrimedSignal just records that some delay activity occurred (set by either).
        if (name == TieredCompilationResumeEvent)
        {
            lock (tieringActivePrimedSignal)
            {
                tieringActiveSignal.Set();
                tieringActivePrimedSignal.Set();
            }
            return;
        }
        if (name == TieredCompilationPauseEvent)
        {
            lock (tieringActivePrimedSignal)
            {
                tieringActiveSignal.Reset();
                tieringActivePrimedSignal.Set();
            }
            return;
        }
        if (name == TieredCompilationBackgroundJitStartEvent)
        {
            // The worker began a batch. Reset idle before setting busy so a reader never sees both set at once.
            backgroundJitIdleSignal.Reset();
            backgroundJitBusySignal.Set();
            return;
        }
        if (name == TieredCompilationBackgroundJitStopEvent)
        {
            HandleBackgroundJitStop(e);
            return;
        }

        if (name.StartsWith(MethodLoadVerbosePrefix, StringComparison.Ordinal))
        {
            HandleMethodLoad(e);
        }
    }

    private void HandleBackgroundJitStop(EventWrittenEventArgs e)
    {
        var payloadNames = e.PayloadNames;
        var payload = e.Payload;
        if (payloadNames is null || payload is null)
            return;

        if (backgroundJitStopPendingIndex < 0)
        {
            backgroundJitStopPendingIndex = payloadNames.IndexOf("PendingMethodCount");
            if (backgroundJitStopPendingIndex < 0)
                return;
        }

        // The worker stopped; once nothing is left queued it has gone idle (its batch — e.g. an OSR'd callee's
        // tier-up — is complete). Reset busy before setting idle so a reader never sees both set at once.
        if (Convert.ToInt64(payload[backgroundJitStopPendingIndex]) == 0)
        {
            backgroundJitBusySignal.Reset();
            backgroundJitIdleSignal.Set();
        }
    }

    private void HandleMethodLoad(EventWrittenEventArgs e)
    {
        var payloadNames = e.PayloadNames;
        var payload = e.Payload;
        if (payloadNames is null || payload is null)
            return;

        if (loadTokenIndex < 0)
        {
            loadTokenIndex = payloadNames.IndexOf("MethodToken");
            loadFlagsIndex = payloadNames.IndexOf("MethodFlags");
            loadNameIndex = payloadNames.IndexOf("MethodName");
            if (loadTokenIndex < 0 || loadFlagsIndex < 0 || loadNameIndex < 0)
                return;
        }

        long tier = (Convert.ToInt64(payload[loadFlagsIndex]) >> OptimizationTierShift) & OptimizationTierMask;

        // A QuickJitted (tier0) publication — for ANY method, not just the one we watch — means an eligible method was
        // just tier0-compiled and is about to run, so its first call will start or join the call-counting delay and a
        // TieredCompilationResume is coming. That is exactly (and all) the up-front gate (WaitForInitialTieringActive)
        // needs: it only asks "is the tiering machinery active, so a Resume will arrive to gate on?", which is a
        // process-wide question. (Pause/Resume prime it too; this also covers the brief window before the first call
        // fires Pause.) The tier0 compile itself is the baseline, not a tier-up, so we never raise a publication for it.
        if (tier == QuickJittedTier0)
        {
            lock (tieringActivePrimedSignal)
            {
                tieringActivePrimedSignal.Set();
            }
            return;
        }

        // An OSR publication is not a step on the call-count tier ladder (it fires off a hot loop's back-edge counter,
        // and the method goes on to be call-count-promoted past it), so don't let it count as a tier-up the stage is
        // waiting on — otherwise a method that OSRs in multiple bodies overruns the stage's publication budget and
        // stops short of the final tier.
        if (tier == OptimizedTier1OSR)
            return;

        // Everything below concerns OUR method reaching its next tier, so filter to it.
        if (Convert.ToInt32(payload[loadTokenIndex]) != metadataToken)
            return;
        if (payload[loadNameIndex] as string != methodName)
            return;

        // Any of the runtime's final tiers means the method is fully warmed and will emit no further tier-ups —
        // whether it tiered all the way up (OptimizedTier1), was compiled straight to optimized code (Optimized), or
        // never tiers at all (MinOptJitted).
        if (tier == OptimizedTier1 || tier == Optimized || tier == MinOptJitted)
            reachedFinalTier = true;

        publicationSignal.Set();
    }

    public override void Dispose()
    {
        // base.Dispose disables the events we enabled (when no other listener wants them).
        base.Dispose();
        publicationSignal.Dispose();
        tieringActivePrimedSignal.Dispose();
        tieringActiveSignal.Dispose();
        backgroundJitBusySignal.Dispose();
        backgroundJitIdleSignal.Dispose();
    }
}
