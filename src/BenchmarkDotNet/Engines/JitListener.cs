using System.Diagnostics.Tracing;
using System.Reflection;
using BenchmarkDotNet.Attributes.CompilerServices;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Engines;

// Observes background JIT tier-up of one or more (benchmark) methods via the runtime's in-process JIT events, so the
// jit stage can proceed once the call tree is actually warm instead of sleeping a fixed delay. There is no API to poll a
// method's tier — the runtime only announces transitions — so we must be listening while they happen. One listener can
// watch several methods; ReachedFinalTier is the aggregate, true only once EVERY watched method is done. (The stage
// watches only the benchmark method today; multi-method support is here so #147 needs no contract change.)
//
// The core signal is JIT QUIESCENCE, not the individual tier-up: a burst tiers up the watched method AND its untracked
// callees on the same background worker, so proceeding the moment the watched method publishes tier1 would race its
// still-compiling callees into the next burst. WaitForQuiescentTierUp instead waits for the worker to go idle and stay
// idle for a settle window, and only then reads the tier counts — by which point the whole tree has landed.
//
// TieredCompilationPause/Resume bracket the call-counting delay, and it matters twice: a burst issued while the delay is
// active isn't counted (the stub install is deferred), and the worker won't compile during it either — so "idle while
// paused" is NOT quiescence. Both waits gate on WaitForTieringActive first.
//
// MethodLoadVerbose is the only per-method event carrying a tier. MethodJittingStarted (compile-began) doesn't, so a
// tier0 compile would be indistinguishable from a tier-up — hence it is deliberately unused.
//
// This is intentionally a per-stage listener: the Jit keyword emits an event for every method jitted process-wide, a
// cost we must not pay during the measurement stages.
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
    // The runtime's stand-in for every reference-type generic argument, since they all share one code body. See
    // WatchedMethod.GetCanonicalTypeName.
    private const string CanonicalReferenceTypeName = "System.__Canon";

    // Optimization tier is packed into MethodFlags bits [7..9]. A method is fully warmed once it reaches a FINAL tier —
    // one from which no further tier-up is coming: OptimizedTier1 (the usual steady state), Optimized (straight to
    // optimized code — AggressiveOptimization, or a loop with TC_QuickJitForLoops off), or MinOptJitted (never tiers at
    // all — NoOptimization, or an optimization-disabled assembly). A non-tiering method therefore publishes exactly one
    // of those and is recognized as done, so we watch every method rather than predicting eligibility from attributes.
    // OptimizedTier1OSR is the exception we ignore (see HandleMethodLoad): it fires off a hot loop's back-edge counter
    // rather than the call-count threshold, so it is never the method's entry-point code version and is orthogonal to
    // the ladder the stage drives. A method that OSRs in both its tier0 and instrumented bodies emits two, which would
    // inflate its tier count and stall the stage short of its final tier.
    private const int OptimizationTierShift = 7;
    private const int OptimizationTierMask = 0x7;
    private const int MinOptJitted = 1;
    private const int Optimized = 2;
    private const int QuickJittedTier0 = 3;
    private const int OptimizedTier1 = 4;
    private const int OptimizedTier1OSR = 5;

    // Added to TieredDelay (rather than a flat cap, so a huge configured delay can't make the cap shorter than the delay
    // itself) before assuming a Resume was dropped — EventPipe sheds events under buffer pressure. Generous vs the
    // ~100ms default, so it only fires on a real drop.
    private static readonly TimeSpan TieringActiveTimeoutMargin = TimeSpan.FromSeconds(1);

    // How long the worker must stay idle to count as quiescent. A burst's methods and their callees tier up in a TRAIN
    // of back-to-back batches a few tens of ms apart, so the window has to bridge those gaps; 30ms does, cheaply.
    private static readonly TimeSpan QuiescenceSettleWindow = TimeSpan.FromMilliseconds(30);
    // How long to let an observed-busy batch drain before assuming its BackgroundJitStop was dropped. Generous: a large
    // compile queue can legitimately take a while, and leaving "busy" stuck would poison every later quiescence check.
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
    private int loadMethodIdIndex = -1;
    private int loadModuleIdIndex = -1;
    private int loadTokenIndex = -1;
    private int loadFlagsIndex = -1;
    private int loadNameIndex = -1;
    private int loadNamespaceIndex = -1;
    private int backgroundJitStopPendingIndex = -1;

    // True when some watched method is canonicalized and so must be matched by name (see WatchedMethod). When none are —
    // the common case — MethodID settles every match and we never touch the name payloads.
    private readonly bool hasCanonicalizedMethod;

    private EventSource? runtimeEventSource;

    private JitListener(WatchedMethod[] methods)
    {
        // The base ctor calls OnEventSourceCreated before this body runs, so these fields are still unset then. That is
        // safe only because the callback merely captures the source without enabling it — enabling would arm
        // OnEventWritten on the dispatcher thread, racing these assignments. (Field initializers run before the base
        // ctor, so only ctor-body assignments are exposed.)
        watchedMethods = methods;
        hasCanonicalizedMethod = Array.Exists(methods, method => method.MatchesByName);
    }

    // Watches every method in the collection. Returns null — so the caller falls back to the fixed delay — when there
    // is nothing to watch, the runtime has no tiered JIT, or EventSource is unavailable.
    internal static JitListener? Create(IEnumerable<MethodInfo> methods)
    {
        MethodInfo[] methodArray = methods as MethodInfo[] ?? methods.ToArray();
        foreach (var method in methodArray)
        {
            // A canonicalized generic method is unidentifiable: the events carry only the declaring type and bare method
            // name, so watching GenMethod<string> would credit GenMethod<int>'s tier-up to it. BDN has never supported
            // generic benchmark methods, so reject rather than half-support them (value-type instantiations happen to be
            // identifiable, reference-type ones never are). Before the IsTiered check, so the contract holds everywhere.
            if (method.IsGenericMethod)
            {
                throw new NotSupportedException(
                    $"Generic methods are not supported as benchmark methods: {method.DeclaringType?.FullName}.{method.Name}");
            }
        }
        if (!JitInfo.IsTiered)
        {
            return null;
        }
        WatchedMethod[] watched;
        try
        {
            watched = methodArray.Select(m => new WatchedMethod(m)).ToArray();
        }
        catch (Exception)
        {
            // A method we can't identify could never be matched to an event: MethodHandle throws for MethodInfos with no
            // runtime MethodDesc (a DynamicMethod, or one from a MetadataLoadContext), which also have no DeclaringType
            // to fall back on. Nothing in-tree passes one, but WorkloadMethods is public API and a best-effort
            // optimization must not take the run down.
            return null;
        }
        if (watched.Length == 0)
        {
            return null;
        }
        var listener = new JitListener(watched);
        if (!listener.Start())
        {
            listener.Dispose();
            return null;
        }
        return listener;
    }

    // True only once EVERY watched method has reached a final tier.
    internal bool ReachedFinalTier => reachedFinalTier;

    // Waits until the call-counting delay is observed inactive, so the stage's first burst is counted. First waits for
    // any sign the tiering machinery is live — a tier0 publication for ANY method, or a Pause/Resume — which guarantees
    // a Resume is coming to gate on. If nothing arrives, tiering is quiet: the watched method was pre-warmed past tier0
    // and no delay is coming on its own, so fake the inactive state and proceed. The lock + IsSet re-check keeps that
    // fake atomic against a real event landing on the timeout boundary; the wait itself is outside the lock so handlers
    // never block on us.
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

    // Waits for the background worker to go quiet, then reports whether every still-tiering watched method advanced
    // beyond `previousTierCounter` (or already reached its final tier). Quiescence is what makes proceeding safe: once
    // the worker is idle, this tier's compiles — the watched methods AND their untracked callees — have all landed. A
    // new batch within the settle window wakes us to drain it and re-settle; a full window with none means the tree is
    // warm. The tier counts are read only AFTER observing idle, so a batch that came and went before we looked has
    // already bumped them. (A caller that has stopped observing ignores the result and uses this only to drain callees.)
    internal bool WaitForQuiescentTierUp(int previousTierCounter, CancellationToken cancellationToken)
    {
        while (true)
        {
            // Wait out any pause first: while paused the worker won't compile even already-enqueued methods, so "idle"
            // isn't quiescent. Returns immediately in the common (not paused) case.
            WaitForTieringActive(cancellationToken);
            if (!WaitForBackgroundJitBusy(QuiescenceSettleWindow, cancellationToken))
            {
                return AllAdvanced(previousTierCounter);
            }
            WaitForBackgroundJitIdle(BackgroundJitDrainTimeout, cancellationToken);
        }
    }

    // Cheaply detects a tier-up while nudging one call at a time: like WaitForQuiescentTierUp but without waiting out a
    // full settle window — it just gives the worker `jitBusyTimeout` to pick the nudge up. The caller settles callees
    // with a final WaitForQuiescentTierUp afterwards.
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

    // Waits for the worker's queue to drain (a BackgroundJitStop with PendingMethodCount == 0). A running batch always
    // finishes, but its Stop can be dropped by EventPipe under buffer pressure — most likely right here, since a busy
    // drain floods the same buffers with MethodLoadVerbose. So on timeout force the idle state: leaving "busy" stuck
    // would poison every later quiescence check.
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

    // Called by the base EventListener constructor (for sources that already exist) — so this runs BEFORE our ctor body
    // and must not do anything that could dispatch an event. Capture only; Start does the enabling.
    protected override void OnEventSourceCreated(EventSource source)
    {
        if (source.Name == RuntimeEventSourceName)
        {
            runtimeEventSource = source;
        }
    }

    // Arms the listener, once the instance is fully constructed — deliberately NOT from OnEventSourceCreated, which the
    // base ctor calls before our ctor body: enabling there would let the dispatcher thread reach OnEventWritten while
    // watchedMethods was still null.
    //
    // Returns whether we can actually observe: EventSource can be compiled out via the
    // System.Diagnostics.Tracing.EventSource.IsSupported feature switch, in which case the enable silently does nothing
    // and IsEnabled stays false. False also covers the runtime source not existing yet — the same "fall back" answer.
    private bool Start()
    {
        var source = runtimeEventSource;
        if (source is null)
        {
            return false;
        }
        EnableEvents(source, EventLevel.Verbose, JitKeyword | CompilationKeyword);
        canObserve = source.IsEnabled(EventLevel.Verbose, JitKeyword | CompilationKeyword);
        return canObserve;
    }

    protected override void OnEventWritten(EventWrittenEventArgs e)
    {
        if (!canObserve)
            return;
        string? name = e.EventName;
        if (name is null)
            return;

        // Pause = a new tier0 method's first call (re)started the delay; Resume = it elapsed and the whole pending list
        // of counting stubs was installed. tieringActiveSignal is the flip-flop the burst gate waits on (Set on Resume =
        // stubs live); tieringActivePrimedSignal just records that some delay activity occurred.
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

        if (loadMethodIdIndex < 0)
        {
            loadMethodIdIndex = payloadNames.IndexOf("MethodID");
            loadModuleIdIndex = payloadNames.IndexOf("ModuleID");
            loadTokenIndex = payloadNames.IndexOf("MethodToken");
            loadFlagsIndex = payloadNames.IndexOf("MethodFlags");
            loadNameIndex = payloadNames.IndexOf("MethodName");
            loadNamespaceIndex = payloadNames.IndexOf("MethodNamespace");
            if (loadMethodIdIndex < 0 || loadModuleIdIndex < 0 || loadTokenIndex < 0
                || loadFlagsIndex < 0 || loadNameIndex < 0 || loadNamespaceIndex < 0)
                return;
        }

        long tier = (Convert.ToInt64(payload[loadFlagsIndex]) >> OptimizationTierShift) & OptimizationTierMask;

        // A tier0 publication for ANY method means one was just compiled and is about to run, so its first call will
        // start or join the call-counting delay and a Resume is coming. That is all the up-front gate needs — "is the
        // tiering machinery live?" is a process-wide question — and it covers the window before that first call fires
        // Pause. The tier0 compile is the baseline, not a tier-up, so it never counts as one.
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

        // OSR is not a step on the call-count ladder (see the tier constants), so counting it would overrun the tier
        // count of a method that OSRs in multiple bodies and stall the stage short of its final tier.
        if (tier == OptimizedTier1OSR)
            return;

        // Everything below concerns one of OUR watched methods reaching its next tier. MethodID settles every
        // non-canonicalized watched method by itself, so only pay for the name payloads when one actually needs them.
        ulong methodId = Convert.ToUInt64(payload[loadMethodIdIndex]);
        ulong moduleId = 0;
        int token = 0;
        string? name = null;
        string? methodNamespace = null;
        if (hasCanonicalizedMethod)
        {
            moduleId = Convert.ToUInt64(payload[loadModuleIdIndex]);
            token = Convert.ToInt32(payload[loadTokenIndex]);
            name = payload[loadNameIndex] as string;
            methodNamespace = payload[loadNamespaceIndex] as string;
        }

        // Any of the runtime's final tiers means the method is fully warmed and will emit no further tier-ups —
        // whether it tiered all the way up (OptimizedTier1), was compiled straight to optimized code (Optimized), or
        // never tiers at all (MinOptJitted).
        bool isFinalTier = tier == OptimizedTier1 || tier == Optimized || tier == MinOptJitted;

        lock (syncRoot)
        {
            if (disposed)
                return;
            // Update EVERY match, not just the first: reference-type generic instantiations share one __Canon code
            // body, so a single publication can legitimately be several watched methods at once (Bench<string> and
            // Bench<object> report one event between them) — they really did all tier up together. Stopping at the
            // first would starve the rest and leave ReachedFinalTier stuck false forever.
            foreach (var candidate in watchedMethods)
            {
                if (!candidate.Matches(methodId, moduleId, token, name, methodNamespace))
                    continue;
                // Count this tier-up so callers can detect the method advanced beyond a given tier.
                candidate.tierUpCount++;
                // Track per-method completion so reachedFinalTier flips only once the LAST watched method is done.
                if (isFinalTier && !candidate.reachedFinalTier)
                {
                    candidate.reachedFinalTier = true;
                    if (++finalTierCount == watchedMethods.Length)
                        reachedFinalTier = true;
                }
            }
        }
    }

    // The event's ModuleID is the address of the module's native Module structure, which RuntimeModule caches in a
    // private m_pData field. Nothing public exposes it (ModuleHandle offers only MDStreamVersion and the Resolve*
    // helpers), and there is no module NAME to compare instead: the event doesn't carry one, ModuleLoad only fires for
    // modules loaded AFTER we start listening — never the already-loaded benchmark module — and two
    // AssemblyLoadContexts loading the same assembly would share a name anyway. So we read the private field, returning
    // 0 ("don't compare") if that ever stops working.
    //
    // A deliberate hack on an implementation detail, acceptable because both ways it can break are safe: renamed/removed
    // yields 0 and we simply stop comparing ModuleID (JitListener_ResolvesModuleId catches it); a wrong non-zero value
    // stops the method matching, which costs the optimization but cannot corrupt a measurement (the canonicalized
    // JitStage_Generic* tests catch it).
    internal static ulong GetModuleId(Module module)
    {
        try
        {
            var field = module.GetType().GetField("m_pData", BindingFlags.NonPublic | BindingFlags.Instance);
            // Guard the type too, so a same-named field with a different meaning can't be read as a pointer.
            if (field is null || field.FieldType != typeof(IntPtr))
            {
                return 0;
            }
            return (ulong) ((IntPtr) field.GetValue(module)!).ToInt64();
        }
        catch
        {
            return 0;
        }
    }

    // Identifies a watched method against MethodLoadVerbose's payload, by one of two strategies.
    //
    // PREFERRED — MethodID, which the runtime reports as the method's MethodDesc pointer, exactly what the public
    // RuntimeMethodHandle.Value gives us. An exact identity (module- and instantiation-proof), stable across tier-ups (a
    // tier-up republishes the same MethodID at a new MethodStartAddress), and one integer compare. Covers everything
    // compiled to its own MethodDesc, i.e. everything not canonicalized — the overwhelming majority of benchmarks.
    //
    // FALLBACK — ModuleID + token + name + canonicalized declaring type, for canonicalized methods, where MethodID is
    // unusable: CoreCLR compiles ONE shared body for all reference-type instantiations and publishes it against the
    // __Canon MethodDesc, which reflection cannot hand us (Bench<string>.MethodHandle is the exact instantiation's, and
    // rebuilding Bench<__Canon> via MakeGenericType just allocates another MethodDesc). Every part is load-bearing:
    // tokens are unique only within a module and every module numbers them from 0x06000001, so an unrelated method
    // elsewhere can share token AND simple name — and with the Jit keyword on, the candidate pool is every method the
    // process jits. A false match would make the stage believe the benchmark tiered up and exit early, silently
    // degrading the very benchmark this class exists to warm. ModuleID closes the last gap (the same assembly loaded
    // twice has identical metadata) and is best-effort: unresolved means "don't compare", no worse than not having it.
    private sealed class WatchedMethod
    {
        // The MethodDesc pointer, or 0 when this method is canonicalized and must be matched by name instead. A real
        // MethodDesc is never null, so 0 is an unambiguous sentinel.
        private readonly ulong methodId;
        // The rest is only populated when methodId is 0. moduleId is 0 when it couldn't be resolved, meaning "don't
        // compare" rather than "compare against 0" — a real Module* is never null either.
        private readonly ulong moduleId;
        private readonly int metadataToken;
        private readonly string? name;
        private readonly string? declaringTypeName;

        internal volatile int tierUpCount;
        internal volatile bool reachedFinalTier;

        internal WatchedMethod(MethodInfo method)
        {
            // Create rejects generic methods, so only the declaring type's arguments can canonicalize us.
            if (IsCanonicalized(method.DeclaringType!))
            {
                moduleId = GetModuleId(method.Module);
                metadataToken = method.MetadataToken;
                name = method.Name;
                declaringTypeName = GetCanonicalTypeName(method.DeclaringType!);
            }
            else
            {
                methodId = (ulong) method.MethodHandle.Value.ToInt64();
            }
        }

        internal bool MatchesByName => methodId == 0;

        internal bool Matches(ulong eventMethodId, ulong eventModuleId, int token, string? eventName, string? eventNamespace)
            => methodId != 0
                ? methodId == eventMethodId
                // Ordered cheapest-first: the token rejects almost every method the process jits before any string compare.
                : metadataToken == token
                    && (moduleId == 0 || moduleId == eventModuleId)
                    && name == eventName
                    && declaringTypeName == eventNamespace;


        // True when the runtime compiles this type's methods into a SHARED (__Canon) body rather than their own
        // MethodDesc — i.e. any generic argument is a reference type, at any depth (Bench<KVP<string, int>> qualifies).
        private static bool IsCanonicalized(Type type)
            => type.IsGenericType && type.GetGenericArguments().Any(IsCanonicalizedArgument);

        private static bool IsCanonicalizedArgument(Type argument)
            => !argument.IsValueType || IsCanonicalized(argument);

        // Renders `type` exactly as MethodLoadVerbose's MethodNamespace spells it, so the two compare by equality. That
        // spelling is the runtime's CANONICAL form — reference-type arguments collapse to System.__Canon (they share one
        // code body), value-type arguments stay exact (they get their own):
        //   Bench<string>, Bench<object> -> My.Ns.Outer+Bench`1[System.__Canon]   (identical by design: ONE shared body
        //                                                                          and one publication really is both —
        //                                                                          hence the multi-match loop above)
        //   Pair<string,int>             -> My.Ns.Outer+Pair`2[System.__Canon,System.Int32]  (≠ Pair<string,long>)
        //   Bench<KVP<string,int>>       -> My.Ns.Outer+Bench`1[...KeyValuePair`2[System.__Canon,System.Int32]]
        // We BUILD the name rather than ask reflection for the canonical Type: MakeGenericType(__Canon) throws on any
        // constrained generic, and can't reach the runtime's shared instantiation anyway — asking for __Canon explicitly
        // just allocates a fresh MethodDesc.
        private static string GetCanonicalTypeName(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.FullName!;
            }
            var arguments = type.GetGenericArguments()
                .Select(argument => argument.IsValueType ? GetCanonicalTypeName(argument) : CanonicalReferenceTypeName);
            return $"{type.GetGenericTypeDefinition().FullName}[{string.Join(",", arguments)}]";
        }
    }

    public override void Dispose()
    {
        lock (syncRoot)
        {
            if (disposed)
                return;
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
