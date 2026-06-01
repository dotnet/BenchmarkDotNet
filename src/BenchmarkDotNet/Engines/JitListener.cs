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
//     tells us when the method reached tier1. The stage keeps invoking until it sees one. (WaitForPublication /
//     ReachedTier1.) We deliberately do NOT use MethodJittingStarted (compile-began): it carries no tier, so the
//     tier0 compile's start is indistinguishable from a tier-up's and would race the tier0 publish that filters it.
//   * TieredCompilationPause/Resume (the call-counting delay bracket, Compilation keyword) gate the bursts: a burst
//     issued while the delay is active isn't counted (the counting stub is deferred), so the stage waits until the
//     delay is observed inactive — a Resume, when the stubs are installed — before bursting (WaitForTieringActive), and
//     up front checks whether any method's tier0 JIT or an already-active delay is underway
//     (WaitForTieringActivePrimed) so it can force one if not. These only avoid wasting bursts — correctness
//     comes from the publication.
//
// This is intentionally a per-stage listener: enabling the Jit keyword emits an event for every method jitted
// process-wide, which we must NOT pay during the measurement stages. It is created at the start of the jit stage
// and disposed at the end.
//
// Create returns null (and the caller falls back to the fixed delay) when EventSource is unavailable — it can be
// disabled via the System.Diagnostics.Tracing.EventSource.IsSupported feature switch — or the method isn't eligible
// for tiered compilation (its assembly has optimizations disabled, or it's pinned to a single optimization level).
internal sealed class JitListener : EventListener
{
    private const string RuntimeEventSourceName = "Microsoft-Windows-DotNETRuntime";
    private const EventKeywords JitKeyword = (EventKeywords)0x10;
    // The "Compilation" keyword carries the TieredCompilation/Pause|Resume events that bracket the runtime's
    // call-counting delay. Low volume (a handful per delay cycle), so enabling it adds no meaningful cost.
    private const EventKeywords CompilationKeyword = (EventKeywords)0x1000000000;
    private const string TieredCompilationResumeEvent = "TieredCompilationResume";
    private const string TieredCompilationPauseEvent = "TieredCompilationPause";
    // Event-name prefix (the runtime appends a version suffix, e.g. MethodLoadVerbose_V2).
    private const string MethodLoadVerbosePrefix = "MethodLoadVerbose";

    // Optimization tier is packed into MethodFlags bits [7..9]: (MethodFlags >> 7) & 0x7.
    // The initial tier0 quick compile is QuickJitted = 3, intermediate instrumented/OSR
    // publications report other values (and just count as "a recompilation happened"),
    // and the fully-optimized steady-state tier1 is OptimizedTier1 = 4.
    private const int OptimizationTierShift = 7;
    private const int OptimizationTierMask = 0x7;
    private const int QuickJittedTier0 = 3;
    private const int OptimizedTier1 = 4;

    private readonly int metadataToken;
    private readonly string methodName;
    private readonly ManualResetEventSlim publicationSignal = new(false);
    private readonly ManualResetEventSlim tieringActiveSignal = new(false);
    private readonly ManualResetEventSlim tieringActivePrimedSignal = new(false);

    private volatile bool reachedTier1;
    private volatile bool canObserve;

    // Cached payload indices (field order is stable within a process for a given event version).
    private int loadTokenIndex = -1;
    private int loadFlagsIndex = -1;
    private int loadNameIndex = -1;

    private JitListener(MethodInfo method)
    {
        // NOTE: the base EventListener ctor calls OnEventSourceCreated before these fields are set,
        // but that callback only enables events / probes canObserve and never reads them.
        metadataToken = method.MetadataToken;
        methodName = method.Name;
    }

    internal static JitListener? Create(MethodInfo method, bool enabled)
    {
        if (!enabled || !JitInfo.IsTiered || !IsTierable(method))
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

    private static bool IsTierable(MethodInfo method)
        => !AreOptimizationsDisabledFor(method)
            && (method.MethodImplementationFlags & (MethodImplAttributes.NoOptimization | CodeGenHelper.AggressiveOptimizationOptionForEmit)) == 0;

    internal static bool AreOptimizationsDisabledFor(MemberInfo member)
        => member.Module.Assembly.GetCustomAttribute<System.Diagnostics.DebuggableAttribute>()?.IsJITOptimizerDisabled ?? false;

    internal bool ReachedTier1 => reachedTier1;

    // Reports (within the timeout) whether the call-counting machinery is active in the process: a tier0 (QuickJitted)
    // publication for ANY method, or a TieredCompilation Pause/Resume — any of which guarantees a Resume is coming to
    // gate on. The stage checks this once before the tier loop: a false result means tiering is quiet and the watched
    // method was likely pre-warmed past tier0, so no delay is coming on its own and one must be forced to get a Resume.
    internal bool WaitForTieringActivePrimed(TimeSpan timeout, CancellationToken cancellationToken)
        => tieringActivePrimedSignal.Wait(timeout, cancellationToken);

    // Waits until the call-counting delay is inactive (a TieredCompilationResume was observed).
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
            tieringActiveSignal.Set();
            tieringActivePrimedSignal.Set();
            return;
        }
        if (name == TieredCompilationPauseEvent)
        {
            tieringActiveSignal.Reset();
            tieringActivePrimedSignal.Set();
            return;
        }

        if (name.StartsWith(MethodLoadVerbosePrefix, StringComparison.Ordinal))
        {
            HandleMethodLoad(e);
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
        // TieredCompilationResume is coming. That is exactly (and all) the up-front gate (WaitForTieringActivePrimed)
        // needs: it only asks "is the tiering machinery active, so a Resume will arrive to gate on?", which is a
        // process-wide question. (Pause/Resume prime it too; this also covers the brief window before the first call
        // fires Pause.) The tier0 compile itself is the baseline, not a tier-up, so we never raise a publication for it.
        if (tier == QuickJittedTier0)
        {
            tieringActivePrimedSignal.Set();
            return;
        }

        // Everything below concerns OUR method reaching its next tier, so filter to it.
        if (Convert.ToInt32(payload[loadTokenIndex]) != metadataToken)
            return;
        if (payload[loadNameIndex] as string != methodName)
            return;

        if (tier == OptimizedTier1)
            reachedTier1 = true;

        publicationSignal.Set();
    }

    public override void Dispose()
    {
        // base.Dispose disables the events we enabled (when no other listener wants them).
        base.Dispose();
        publicationSignal.Dispose();
        tieringActivePrimedSignal.Dispose();
        tieringActiveSignal.Dispose();
    }
}
