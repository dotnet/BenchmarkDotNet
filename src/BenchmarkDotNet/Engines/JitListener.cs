using System.Diagnostics.Tracing;
using System.Reflection;
using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Engines;

// Observes background JIT tier-up of one or more (benchmark) methods by listening to the runtime's JIT events
// in-process, so the jit stage can proceed as soon as the call tree is actually warmed instead of waiting a fixed
// delay. The runtime only announces transitions (there is no API to poll a method's current tier), so we must be
// listening while they happen. A single listener can watch several methods at once — ReachedFinalTier is the aggregate
// (true only once EVERY watched method has reached its final tier). (Today the stage watches just the benchmark method;
// watching multiple is in place so scenarios that drive several methods need no contract change. #147)
//
// The core signal is JIT QUIESCENCE, not the individual tier-up. A burst tiers up the watched method AND its (untracked)
// callees on the same background worker; proceeding the instant the watched method publishes tier1 would leave its
// callees still compiling and race them into the next burst / the following stage. So each tier the stage waits for the
// background worker to go idle (WaitForQuiescentTierUp): once it's quiet the whole tree reached this tier, and the
// watched methods' tier counts can be read race-free.
//
// The events it watches, and their roles:
//   * TieredCompilationBackgroundJitStart/Stop (Compilation keyword) bracket the background tiering worker draining its
//     queue — Start when it begins, Stop when it finishes (Stop's PendingMethodCount payload is how many remain; 0 =
//     drained). These fire ONLY for actual tiered background work, so they are how we detect quiescence: a burst's
//     methods and their callees tier up in a train of back-to-back batches, and WaitForQuiescentTierUp waits for the
//     worker to be idle and STAY idle for a short settle window. A batch that began-and-finished before we looked is not
//     lost — it already bumped the tier counts, which we only read after observing the worker idle (see MethodLoadVerbose).
//   * MethodLoadVerbose (per-method, JIT keyword) reports each tier publication and carries the tier. A non-tier0 load
//     for a watched method bumps its tier-up count (so callers can detect it advanced beyond a given tier) and, when the
//     tier is a final one, marks it done. We deliberately do NOT use MethodJittingStarted (compile-began): it carries no
//     tier, so the tier0 compile's start is indistinguishable from a tier-up's and would race the tier0 publish that
//     filters it.
//   * TieredCompilationPause/Resume (the tiering delay bracket, Compilation keyword) bound the call-counting delay. Two
//     roles: (1) a burst issued while the delay is active isn't counted (the counting stub is deferred), so the stage
//     waits until the delay is observed inactive — a Resume — before bursting (WaitForTieringActive); up front
//     (WaitForInitialTieringActive) it waits for any method's tier0 JIT or a Pause/Resume to confirm a Resume is coming,
//     and if none arrives the method was pre-warmed so it fakes the inactive state and proceeds. (2) While the delay is
//     active the background worker is paused — it won't compile even already-enqueued tier-ups until the Resume — so
//     "worker idle" during a pause is NOT quiescence. WaitForQuiescentTierUp therefore calls WaitForTieringActive each
//     loop turn to wait the pause out before timing the idle settle window.
//
// The busy/idle state is a ManualResetEventSlim pair; the per-method tier/completion counts are mutated under syncRoot
// (which OnEventWritten already holds) but declared volatile so the waiters read them lock-free — their ordering comes
// from draining the in-flight batch first, not from the lock. So the waiters are lock-free (the events handle their own
// timeouts and cancellation), and WaitForTieringActive composes naturally into the quiescence loop.
//
// This is intentionally a per-stage listener: enabling the Jit keyword emits an event for every method jitted
// process-wide, which we must NOT pay during the measurement stages. It is created at the start of the jit stage
// and disposed at the end.
//
// Create returns null (and the caller falls back to the fixed delay) when the runtime has no tiered JIT, or when
// EventSource is unavailable — it can be disabled via the System.Diagnostics.Tracing.EventSource.IsSupported feature
// switch. It otherwise watches each method regardless of whether it looks tier-eligible: a method that can't tier just
// publishes its single final tier (see the tier constants below), which the stage observes and treats as "done".
[AggressivelyOptimizeMethods] // Reduce JIT event noise from the listener itself.
internal sealed class JitListener : EventListener
{
    private const string RuntimeEventSourceName = "Microsoft-Windows-DotNETRuntime";
    private const EventKeywords JitKeyword = (EventKeywords)0x10;
    // The "Compilation" keyword carries the TieredCompilation/Pause|Resume and BackgroundJit Start/Stop events. Low
    // volume (a handful per delay/batch cycle), so enabling it adds no meaningful cost.
    private const EventKeywords CompilationKeyword = (EventKeywords)0x1000000000;
    private const string TieredCompilationResumeEvent = "TieredCompilationResume";
    private const string TieredCompilationPauseEvent = "TieredCompilationPause";
    // The background tiering worker brackets each batch with these: Start when it begins draining its queue, Stop when
    // it finishes (Stop's PendingMethodCount payload is how many remain — 0 = drained). They are the quiescence signal.
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
    // HandleMethodLoad) so they don't inflate its tier count and stall the stage short of the final tier.
    private const int OptimizationTierShift = 7;
    private const int OptimizationTierMask = 0x7;
    private const int MinOptJitted = 1;
    private const int Optimized = 2;
    private const int QuickJittedTier0 = 3;
    private const int OptimizedTier1 = 4;
    private const int OptimizedTier1OSR = 5;

    // Margin added on top of the call-counting delay when waiting for a TieredCompilationResume, before assuming it
    // was dropped (EventPipe sheds events under buffer pressure) and proceeding as if the delay had elapsed. We add it
    // to TieredDelay rather than use a flat cap so a deliberately huge delay can't make the cap shorter than the delay
    // itself. Generous vs the ~100ms default delay, so it only ever fires on an actual drop, not on the normal path.
    private static readonly TimeSpan TieringActiveTimeoutMargin = TimeSpan.FromSeconds(1);

    // How long the background worker must stay idle (no new batch) for us to declare quiescence. A burst's methods and
    // their callees tier up in a TRAIN of back-to-back background batches (a few tens of ms apart), so the window has to
    // bridge those gaps and only conclude "quiet" once a full window passes with no new batch. 30ms comfortably spans
    // the inter-batch gap while keeping the per-tier settle cost small.
    private static readonly TimeSpan QuiescenceSettleWindow = TimeSpan.FromMilliseconds(30);
    // How long to wait for an observed-busy background JIT batch to drain before assuming its BackgroundJitStop was
    // dropped (EventPipe sheds events under pressure) and proceeding. Generous — it only bites on a dropped Stop, and a
    // large compile queue can legitimately take a while; leaving "busy" stuck would poison every later quiescence check.
    private static readonly TimeSpan BackgroundJitDrainTimeout = TimeSpan.FromSeconds(10);

    // One entry per watched method. Small (a handful at most), so HandleMethodLoad scans it linearly per publication.
    private readonly WatchedMethod[] watchedMethods;
    private readonly object syncRoot = new();
    private readonly ManualResetEventSlim tieringActiveSignal = new(false);
    private readonly ManualResetEventSlim tieringActivePrimedSignal = new(false);
    // The background tiering worker's busy/idle state (Start..Stop-with-0-pending). Kept as a paired flip-flop so a
    // reader never sees both set at once. Set/Reset under syncRoot.
    private readonly ManualResetEventSlim backgroundJitBusySignal = new(false);
    private readonly ManualResetEventSlim backgroundJitIdleSignal = new(true);

    // Number of watched methods that have reached a final tier (guarded by syncRoot). reachedFinalTier mirrors
    // "finalTierCount == watchedMethods.Length" for a lock-free read; both flip together once every method is done.
    private int finalTierCount;
    private volatile bool reachedFinalTier;
    private volatile bool canObserve;
    private bool disposed;

    // Cached payload indices (field order is stable within a process for a given event version).
    private int loadTokenIndex = -1;
    private int loadFlagsIndex = -1;
    private int loadNameIndex = -1;
    private int backgroundJitStopPendingIndex = -1;

    private JitListener(WatchedMethod[] methods)
    {
        // NOTE: the base EventListener ctor calls OnEventSourceCreated before this field is set, but that callback only
        // enables events / probes canObserve and never reads watchedMethods.
        watchedMethods = methods;
    }

    // Watches every method in the collection. Returns null — so the caller falls back to the fixed delay — when there
    // is nothing to watch, the runtime has no tiered JIT, or EventSource is unavailable.
    internal static JitListener? Create(IEnumerable<MethodInfo> methods)
    {
        if (!JitInfo.IsTiered)
        {
            return null;
        }
        var watched = methods.Select(m => new WatchedMethod(m)).ToArray();
        if (watched.Length == 0)
        {
            return null;
        }
        var listener = new JitListener(watched);
        if (!listener.canObserve)
        {
            listener.Dispose();
            return null;
        }
        return listener;
    }

    // True only once EVERY watched method has reached a final tier.
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
            lock (syncRoot)
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
    // the tier loop after WaitForInitialTieringActive established the delay was inactive up front. Bounded: a Resume can
    // be dropped by EventPipe under buffer pressure, so rather than block forever we wait up to TieredDelay plus a margin and
    // then assume the delay elapsed (stubs installed) and proceed — the same fallback WaitForInitialTieringActive uses.
    // The cap only bites on a dropped event; a real Resume normally arrives within the call-counting delay (~100ms).
    internal void WaitForTieringActive(CancellationToken cancellationToken)
    {
        // No call-counting delay (e.g. AggressiveTiering) — counting is armed immediately, nothing to gate on.
        if (JitInfo.TieredDelay <= TimeSpan.Zero)
        {
            return;
        }
        if (!tieringActiveSignal.Wait(JitInfo.TieredDelay + TieringActiveTimeoutMargin, cancellationToken))
        {
            // The primed signal is already set (WaitForInitialTieringActive ran first), so only flip the active
            // signal. Lock so this can't interleave with a concurrent Pause/Resume handler.
            lock (syncRoot)
            {
                tieringActiveSignal.Set();
            }
        }
    }

    // Waits for the background tiering worker to go quiet, then reports whether every still-tiering watched method has
    // advanced beyond `previousTierCounter` tier-ups (or already reached its final tier). Quiescence is what makes
    // proceeding safe: when the worker is idle, this tier's compiles (the watched method(s) AND their untracked callees,
    // which tier up in a train of back-to-back batches) have all landed, so the next burst / the following stage won't
    // race them. We wait for the worker to be idle and STAY idle for a settle window — a new batch within the window (a
    // callee tiering up, or the watched-method batch starting after a slow enqueue) wakes us to drain it and re-settle;
    // a full window with no new batch means the tree is warm. We read the (volatile) tier counts only AFTER observing
    // the worker idle, so a batch that started-and-finished before we looked has already bumped them. (When the caller
    // has stopped observing the watched methods it ignores the result and just uses this to drain untracked callees.)
    internal bool WaitForQuiescentTierUp(int previousTierCounter, CancellationToken cancellationToken)
    {
        while (true)
        {
            // Wait out any tiering pause first: while paused, the worker won't compile even already-enqueued methods, so
            // "idle" isn't quiescent — WaitForTieringActive returns once counting is active again (the delay elapsed /
            // a Resume was observed). In the common case (not paused) it returns immediately.
            WaitForTieringActive(cancellationToken);
            // Wait the settle window for the worker to (re)start a batch. A burst's methods and their callees tier up in
            // a train of back-to-back batches, so if one starts we drain it and re-check; if none starts within the
            // window the tree has settled.
            if (!WaitForBackgroundJitBusy(QuiescenceSettleWindow, cancellationToken))
            {
                return AllAdvanced(previousTierCounter);
            }
            WaitForBackgroundJitIdle(BackgroundJitDrainTimeout, cancellationToken);
        }
    }

    // Waits up to the timeout for every still-tiering watched method to advance beyond `previousTierCounter`, WITHOUT
    // waiting out a full settle window. Used to cheaply detect a tier-up while nudging one call at a time: it returns
    // immediately if they've already advanced, otherwise waits out any pause and gives the worker a short window
    // (jitBusyTimeout) to pick the nudge up, draining it if it does. The caller does a final WaitForQuiescentTierUp
    // afterwards to settle callees. True if all advanced; false otherwise.
    internal bool WaitForTierUp(int previousTierCounter, TimeSpan jitBusyTimeout, CancellationToken cancellationToken)
    {
        if (AllAdvanced(previousTierCounter))
        {
            return true;
        }
        WaitForTieringActive(cancellationToken);
        if (WaitForBackgroundJitBusy(jitBusyTimeout, cancellationToken))
        {
            WaitForBackgroundJitIdle(BackgroundJitDrainTimeout, cancellationToken);
        }
        return AllAdvanced(previousTierCounter);
    }

    // Waits up to the timeout for the background tiering worker to be running a batch. True if it is/becomes busy.
    private bool WaitForBackgroundJitBusy(TimeSpan timeout, CancellationToken cancellationToken)
        => backgroundJitBusySignal.Wait(timeout, cancellationToken);

    // Waits (up to the timeout) for the background tiering worker to go idle (its queue drained — a BackgroundJitStop
    // with PendingMethodCount == 0). The caller only waits after observing the worker busy, and a running batch always
    // finishes — but the BackgroundJitStop can be dropped by EventPipe under buffer pressure (most likely right here,
    // since a busy drain floods the same buffers with MethodLoadVerbose). So on timeout we force the idle state and
    // proceed: leaving "busy" stuck would poison every later quiescence check. The cap only bites on a dropped Stop.
    private void WaitForBackgroundJitIdle(TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (!backgroundJitIdleSignal.Wait(timeout, cancellationToken))
        {
            lock (syncRoot)
            {
                // Reset busy before setting idle so a reader never sees both set at once (matches HandleBackgroundJitStop).
                backgroundJitBusySignal.Reset();
                backgroundJitIdleSignal.Set();
            }
        }
    }

    // Whether every watched method either advanced its tier count or already reached its final tier.
    private bool AllAdvanced(int previousTierCount)
    {
        for (int i = 0; i < watchedMethods.Length; i++)
        {
            var method = watchedMethods[i];
            if (!method.reachedFinalTier && method.tierUpCount <= previousTierCount)
            {
                return false;
            }
        }
        return true;
    }

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
            lock (syncRoot)
            {
                if (disposed)
                    return;
                tieringActiveSignal.Set();
                tieringActivePrimedSignal.Set();
            }
            return;
        }
        if (name == TieredCompilationPauseEvent)
        {
            lock (syncRoot)
            {
                if (disposed)
                    return;
                tieringActiveSignal.Reset();
                tieringActivePrimedSignal.Set();
            }
            return;
        }
        if (name == TieredCompilationBackgroundJitStartEvent)
        {
            lock (syncRoot)
            {
                if (disposed)
                    return;
                // The worker began a batch (the watched method(s) and/or untracked callees tiering up). Reset idle
                // before setting busy so a reader never sees both set at once.
                backgroundJitIdleSignal.Reset();
                backgroundJitBusySignal.Set();
            }
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

        // The worker stopped; once nothing is left queued the batch is fully drained and the JIT is idle.
        if (Convert.ToInt64(payload[backgroundJitStopPendingIndex]) == 0)
        {
            lock (syncRoot)
            {
                if (disposed)
                    return;
                // Reset busy before setting idle so a reader never sees both set at once.
                backgroundJitBusySignal.Reset();
                backgroundJitIdleSignal.Set();
            }
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
        // fires Pause.) The tier0 compile itself is the baseline, not a tier-up, so we never count it.
        if (tier == QuickJittedTier0)
        {
            lock (syncRoot)
            {
                if (disposed)
                    return;
                tieringActivePrimedSignal.Set();
            }
            return;
        }

        // An OSR publication is not a step on the call-count tier ladder (it fires off a hot loop's back-edge counter,
        // and the method goes on to be call-count-promoted past it), so don't let it count as a tier-up — otherwise a
        // method that OSRs in multiple bodies overruns its tier count and the stage stops short of the final tier.
        if (tier == OptimizedTier1OSR)
            return;

        // Everything below concerns one of OUR watched methods reaching its next tier, so find the matching one (if any).
        int token = Convert.ToInt32(payload[loadTokenIndex]);
        string? name = payload[loadNameIndex] as string;
        WatchedMethod? matched = null;
        foreach (var candidate in watchedMethods)
        {
            if (candidate.MetadataToken == token && candidate.Name == name)
            {
                matched = candidate;
                break;
            }
        }
        if (matched is null)
            return;

        // Any of the runtime's final tiers means the method is fully warmed and will emit no further tier-ups —
        // whether it tiered all the way up (OptimizedTier1), was compiled straight to optimized code (Optimized), or
        // never tiers at all (MinOptJitted).
        bool isFinalTier = tier == OptimizedTier1 || tier == Optimized || tier == MinOptJitted;

        lock (syncRoot)
        {
            if (disposed)
                return;
            // Count this tier-up so callers can detect the method advanced beyond a given tier.
            matched.tierUpCount++;
            // Track per-method completion so reachedFinalTier flips only once the LAST watched method is done.
            if (isFinalTier && !matched.reachedFinalTier)
            {
                matched.reachedFinalTier = true;
                if (++finalTierCount == watchedMethods.Length)
                    reachedFinalTier = true;
            }
        }
    }

    private sealed class WatchedMethod(MethodInfo method)
    {
        internal volatile int tierUpCount;
        internal volatile bool reachedFinalTier;

        internal int MetadataToken => method.MetadataToken;
        internal string Name => method.Name;
    }

    public override void Dispose()
    {
        lock (syncRoot)
        {
            disposed = true;
        }
        // base.Dispose disables the events we enabled (when no other listener wants them).
        base.Dispose();
        tieringActivePrimedSignal.Dispose();
        tieringActiveSignal.Dispose();
        backgroundJitBusySignal.Dispose();
        backgroundJitIdleSignal.Dispose();
    }
}
