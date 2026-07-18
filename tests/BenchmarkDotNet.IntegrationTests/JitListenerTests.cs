using System.Reflection;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.XUnit;
using Perfolizer.Horology;

namespace BenchmarkDotNet.IntegrationTests;

public class JitListenerTests
{
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_Cold()
    {
        Func<long, long> workloadMethod = Cold;

        using var observer = JitListener.Create([workloadMethod.Method]);

        RunJitStageToCompletion(workloadMethod, observer);

        AssertReachedFinalTier(observer);
    }

    // Tests InProcess benchmarking the same method multiple times: run 2's stage sees an ALREADY-tier1 method under a
    // fresh listener (reusing observer would take its ReachedFinalTier shortcut and skip this path entirely).
    // Only run 1 is asserted: an already-tier1 method emits no events, so asserting observer2 would fail by
    // construction. Run 2 asserts nothing — it verifies the stage COMPLETES rather than waiting forever for a tier-up
    // that is never coming (an earlier design hung exactly here), enforced by RunJitStageToCompletion's cancellation.
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_AlreadyTier1()
    {
        Func<long, long> workloadMethod = AlreadyTier1;

        using var observer = JitListener.Create([workloadMethod.Method]);

        RunJitStageToCompletion(workloadMethod, observer);
        using var observer2 = JitListener.Create([workloadMethod.Method]);
        RunJitStageToCompletion(workloadMethod, observer2);

        AssertReachedFinalTier(observer);
    }

    // Tests the case of InProcess benchmarking a method that the user already invoked before starting the benchmarks when call counting is active.
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_AlreadyTier0()
    {
        Func<long, long> workloadMethod = AlreadyTier0;
        // Watch from before the pre-invoke, and hand this listener to the stage so it doesn't create a second one
        // (see RunJitStageToCompletion): in a minopt build the pre-invoke is the method's only compile.
        using var observer = JitListener.Create([workloadMethod.Method]);

        DeadCodeEliminationHelper.KeepAliveWithoutBoxing(AlreadyTier0(42));
        // Sleep long enough for the tiered call counting to begin.
        Engine.SleepIfPositive(JitInfo.TieredDelay + JitInfo.TieredDelay);

        RunJitStageToCompletion(workloadMethod, observer);

        AssertReachedFinalTier(observer);
    }

    // Tests the case of InProcess benchmarking a method that the user already invoked before starting the benchmarks when call counting is delayed.
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_AlreadyTier0DelayedCallCounting()
    {
        Func<long, long> workloadMethod = AlreadyTier0DelayedCallCounting;
        // Watch from before the pre-invoke, and hand this listener to the stage (see RunJitStageToCompletion). We do
        // NOT sleep first: this test's whole point is that the call-counting delay is still pending when the stage
        // starts. Because the stage reuses this one listener, the pre-invoke's event is never lost to a second
        // listener's session churn, so no wait is needed to observe the final tier.
        using var observer = JitListener.Create([workloadMethod.Method]);

        DeadCodeEliminationHelper.KeepAliveWithoutBoxing(AlreadyTier0DelayedCallCounting(42));

        RunJitStageToCompletion(workloadMethod, observer);

        AssertReachedFinalTier(observer);
    }

    // Tests a benchmark method whose own hot loop is On-Stack-Replaced (OSR) mid-execution. Where OSR is enabled
    // (by default in .NET 7+) this drives the method through an OSR publication on top of its normal tier-ups, and the
    // stage must still reach OptimizedTier1 — JitInfo.MaxTierPromotions reserves an extra promotion for the OSR-induced
    // double tier0-instrumentation. Where OSR is off it is simply a hot-loop method that tiers up normally; either way
    // it ends at tier1.
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_Osr()
    {
        Func<long, long> workloadMethod = Osr;

        using var observer = JitListener.Create([workloadMethod.Method]);

        RunJitStageToCompletion(workloadMethod, observer);

        AssertReachedFinalTier(observer);
    }

    // Tests a benchmark method that calls (without inlining) a separate method whose hot loop is OSR'd. The listener
    // only watches the benchmark method, never the callee, so it can't observe the callee's tiering at all — this
    // exercises the runtime bug where an OSR'd callee gets tier0-instrumented twice (JitInfo.MaxTierPromotions reserves
    // the extra promotion the stage spends on it). The benchmark method itself must still be driven to OptimizedTier1.
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_CallsOsr()
    {
        Func<long, long> workloadMethod = CallsOsr;
        Func<long, long> calleeMethod = OsrCallee;

        using var observer = JitListener.Create([workloadMethod.Method]);
        // The stage only drives (and the engine's listener only watches) the benchmark method, but every call to it
        // calls the OSR'd callee, so the callee should be driven all the way to tier1 too. Watch it independently.
        using var calleeObserver = JitListener.Create([calleeMethod.Method]);

        RunJitStageToCompletion(workloadMethod, observer);

        AssertReachedFinalTier(observer);
        AssertReachedFinalTier(calleeObserver);
    }

    // A method pinned to a single optimization level never tiers, but the listener still watches it and recognizes
    // its one-and-only compile as a final tier (MinOptJitted or Optimized) — so the stage observes "done" rather than
    // depending on an attribute heuristic to decline up front.

    // [MethodImpl(NoOptimization)] pins the method to minopts, so its final tier is MinOptJitted.
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_NoOptimization()
    {
        Func<long, long> workloadMethod = NoOptimization;

        using var observer = JitListener.Create([workloadMethod.Method]);

        RunJitStageToCompletion(workloadMethod, observer);

        AssertReachedFinalTier(observer);
    }

    // [MethodImpl(AggressiveOptimization)] pins the method straight to optimized code, so it never goes through
    // tier0 -> tier1 and its final tier is Optimized (or OptimizedTier1, depending on runtime).
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_AggressiveOptimization()
    {
        Func<long, long> workloadMethod = AggressiveOptimization;

        using var observer = JitListener.Create([workloadMethod.Method]);

        RunJitStageToCompletion(workloadMethod, observer);

        AssertReachedFinalTier(observer);
    }

    // A single listener can watch several methods at once. Here one listener watches two distinct methods, and the
    // stage drives both (the workload action calls each per invocation). ReachedFinalTier is the aggregate, so it only
    // becomes true once BOTH have reached their final tier — exercising the multi-method path that scenarios like
    // dotnet/BenchmarkDotNet#147 (driving several methods) will rely on.
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_MultipleMethods()
    {
        Func<long, long> first = MultiFirst;
        Func<long, long> second = MultiSecond;

        using var observer = JitListener.Create([first.Method, second.Method]);

        RunJitStageToCompletion(observer, [first.Method, second.Method], i => { first(i); second(i); });

        AssertReachedFinalTier(observer);
    }

    // Watched methods need not be invoked at the same rate (e.g. #147 may call one more often than another).
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_MultipleMethodsUnevenInvocationRates()
    {
        Func<long, long> fast = MultiUnevenFast;
        Func<long, long> slow = MultiUnevenSlow;

        using var observer = JitListener.Create([fast.Method, slow.Method]);

        // Each time the fast method has accumulated a tier's worth of calls it is due to tier up, so pause well past the
        // ~100ms call-counting delay to let the background JIT actually publish that tier-up before the slow method
        // reaches its own threshold. Staggering the two methods' publications is the entire point of the test —
        // without it they tier up together and this is just JitStage_MultipleMethods with extra steps. Stop after
        // MaxTierPromotions crossings: the fast method is fully warmed by then, so more sleeping would only cost time.
        int fastCallsSinceCrossing = 0;
        int fastTierCrossings = 0;
        RunJitStageToCompletion(observer, [fast.Method, slow.Method], i =>
        {
            fast(i);
            fast(i);
            fast(i);
            fastCallsSinceCrossing += 3;
            if (fastTierCrossings < JitInfo.MaxTierPromotions && fastCallsSinceCrossing >= JitInfo.TieredCallCountThreshold)
            {
                ++fastTierCrossings;
                fastCallsSinceCrossing = 0;
                Thread.Sleep(200);
            }
            slow(i);
        });

        Assert.Equal(JitInfo.MaxTierPromotions, fastTierCrossings);
        AssertReachedFinalTier(observer);
    }

    // Generic benchmark types ([GenericTypeArguments]) are where identifying the method is hardest: every instantiation
    // of GenericBench<T>.Method shares one metadata token AND one simple name. See WatchedMethod.

    // Value-type arguments aren't canonicalized, so this takes the exact MethodID path.
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_GenericTypeWithValueTypeArgument()
    {
        Func<long, long> workloadMethod = GenericBench<int>.Method;

        using var observer = JitListener.Create([workloadMethod.Method]);

        RunJitStageToCompletion(workloadMethod, observer);

        AssertReachedFinalTier(observer);
    }

    // A reference-type argument is canonicalized: CoreCLR publishes the shared __Canon body, which reflection can't hand
    // us, so this takes the name-matching path. If the runtime's spelling ever drifts from ours, matching silently stops
    // and this fails.
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_GenericTypeWithReferenceTypeArgument()
    {
        Func<long, long> workloadMethod = GenericBench<string>.Method;

        using var observer = JitListener.Create([workloadMethod.Method]);

        RunJitStageToCompletion(workloadMethod, observer);

        AssertReachedFinalTier(observer);
    }

    // Generic methods are rejected rather than half-supported — see JitListener.Create. Ungated, because the check runs
    // before the tiered-JIT test: this is the one case that also runs on net472.
    [Theory]
    [InlineData(typeof(int))]    // identifiable, but rejected anyway
    [InlineData(typeof(string))] // never identifiable
    public void JitListener_ThrowsForGenericMethod(Type argument)
    {
        MethodInfo genericMethod = typeof(JitListenerTests)
            .GetMethod(nameof(GenericMethod), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(argument);

        Assert.Throws<NotSupportedException>(() => JitListener.Create([genericMethod]));
    }

    // Two reference-type instantiations watched at once. CoreCLR compiles ONE __Canon body for both, and publishes one
    // event for it, so that single event legitimately is both watched methods — they really did tier up together.
    // ReachedFinalTier (the aggregate) must still become true; matching only the first would strand the second forever.
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_GenericTypeWithTwoReferenceTypeArguments()
    {
        Func<long, long> first = SharedGenericBench<string>.Method;
        Func<long, long> second = SharedGenericBench<object>.Method;

        using var observer = JitListener.Create([first.Method, second.Method]);

        RunJitStageToCompletion(observer, [first.Method, second.Method], i => { first(i); second(i); });

        AssertReachedFinalTier(observer);
    }

    // A mix of reference-type and value-type arguments: the reference one canonicalizes to __Canon while the value one
    // stays exact, so this pins down the multi-argument rendering rather than just the single-argument case.
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_GenericTypeWithMixedArguments()
    {
        Func<long, long> workloadMethod = PairBench<string, int>.Method;

        using var observer = JitListener.Create([workloadMethod.Method]);

        RunJitStageToCompletion(workloadMethod, observer);

        AssertReachedFinalTier(observer);
    }

    // False-positive guards, one per matching strategy: watch one instantiation, drive a different one sharing its token
    // AND simple name. Crediting the driven method's tier-up to the watched one would exit the jit stage believing a
    // never-run method was warm. Stand-ins are private to these tests so nothing else can tier them up.

    // Value-type instantiations aren't canonicalized -> exact MethodID path.
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_DistinctValueTypeInstantiationsAreNotConfused()
    {
        Func<long, long> watched = DistinctBench<int>.Method;
        Func<long, long> driven = DistinctBench<long>.Method;

        using var observer = JitListener.Create([watched.Method]);

        RunJitStageToCompletion(observer, [driven.Method], i => driven(i));

        Assert.NotNull(observer);
        Assert.False(observer.ReachedFinalTier,
            "DistinctBench<long>'s tier-up must not be credited to the watched DistinctBench<int>");
    }

    // The same guard on the name-matching path: both are canonicalized, so only the declaring type name separates them
    // (...[System.__Canon,System.Int32] vs ...[System.__Canon,System.Int64]).
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_DistinctCanonicalizedInstantiationsAreNotConfused()
    {
        Func<long, long> watched = DistinctPairBench<string, int>.Method;
        Func<long, long> driven = DistinctPairBench<string, long>.Method;

        using var observer = JitListener.Create([watched.Method]);

        RunJitStageToCompletion(observer, [driven.Method], i => driven(i));

        Assert.NotNull(observer);
        Assert.False(observer.ReachedFinalTier,
            "DistinctPairBench<string, long>'s tier-up must not be credited to the watched DistinctPairBench<string, int>");
    }

    // Overloads share a simple name AND a declaring type, so they are told apart only by their metadata token (each
    // overload is its own MethodDef row). These guard that, once per matching strategy: watch one overload, drive the
    // other, and require the driven one's tier-up not to be credited to the watched one.

    // Non-generic, so matched by MethodID — each overload has its own MethodDesc.
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_OverloadsAreNotConfused()
    {
        Func<long, long> watched = OverloadBench.Method;
        Func<int, long> driven = OverloadBench.Method;

        using var observer = JitListener.Create([watched.Method]);

        RunJitStageToCompletion(observer, [driven.Method], i => driven((int) i));

        Assert.NotNull(observer);
        Assert.False(observer.ReachedFinalTier,
            "OverloadBench.Method(int)'s tier-up must not be credited to the watched OverloadBench.Method(long)");
    }

    // Canonicalized, so matched by name — where both overloads share the name AND the declaring type, leaving the
    // metadata token as the only discriminator.
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_CanonicalizedOverloadsAreNotConfused()
    {
        Func<long, long> watched = OverloadPairBench<string, int>.Method;
        Func<int, long> driven = OverloadPairBench<string, int>.Method;

        using var observer = JitListener.Create([watched.Method]);

        RunJitStageToCompletion(observer, [driven.Method], i => driven((int) i));

        Assert.NotNull(observer);
        Assert.False(observer.ReachedFinalTier,
            "OverloadPairBench<string, int>.Method(int)'s tier-up must not be credited to the watched Method(long) overload");
    }

    // GetModuleId reflects on a private runtime field, and degrades silently to "don't compare" if it ever disappears —
    // so this is the only test that can notice, and the only signal that the hack still works on a new runtime. (A
    // wrong-but-non-zero value is caught instead by the canonicalized JitStage_Generic* tests, which stop matching.)
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitListener_ResolvesModuleId()
    {
        Assert.NotEqual(0UL, JitListener.GetModuleId(typeof(JitListenerTests).Module));
    }

    private static void AssertReachedFinalTier(JitListener? observer)
    {
        // No wait needed: the tier-up event is delivered while the stage is still running (it spans hundreds of ms of
        // tiering delays), so by the time the stage returns the observer has already recorded the final tier.
        Assert.NotNull(observer);
        Assert.True(observer.ReachedFinalTier, "the jit stage should have driven the benchmark method to its final tier");
    }

    // The test owns the listener and passes it in; the stage uses that exact instance (it never creates its own), so
    // there is never a second EventListener whose setup/teardown could flush an event in flight to the test's listener.
    private static void RunJitStageToCompletion(Func<long, long> workloadMethod, JitListener? listener)
        => RunJitStageToCompletion(listener, [workloadMethod.Method], i => workloadMethod(i));

    // Core harness: the stage watches/drives the given workloadMethods, and each iteration runs invokeOnce (which the
    // caller wires to actually call those methods) invokeCount times so they go through call counting and tier up.
    private static void RunJitStageToCompletion(JitListener? listener, MethodInfo[] workloadMethods, Action<long> invokeOnce)
    {
        // The stage's per-tier quiescence wait loops for as long as the background JIT keeps starting new batches, and is
        // cancellable only via the host's token. In practice it settles in tens of ms, so this never fires — it is here so
        // that a stall (or a listener bug that waits for a tier-up which is never coming, as JitStage_AlreadyTier1 guards
        // against) fails this one test instead of wedging the whole run.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var host = new CancellableHost(timeout.Token);
        Func<long, IClock, ValueTask<ClockSpan>> empty = (_, _) => new(default(ClockSpan));
        Func<long, IClock, ValueTask<ClockSpan>> workload = (invokeCount, _) =>
        {
            // Really invoke the benchmark method(s) so they go through call counting and tier up for real.
            for (long i = 0; i < invokeCount; i++)
                invokeOnce(i);
            return new(default(ClockSpan));
        };

        var parameters = new EngineParameters
        {
            Host = host,
            WorkloadMethods = workloadMethods,
            WorkloadActionNoUnroll = workload,
            WorkloadActionUnroll = workload,
            OverheadActionNoUnroll = empty,
            OverheadActionUnroll = empty,
            GlobalSetupAction = () => new(),
            GlobalCleanupAction = () => new(),
            IterationSetupAction = () => new(),
            IterationCleanupAction = () => new(),
            TargetJob = Job.Default,
            BenchmarkName = "",
            InProcessDiagnoserHandler = new([], host, BenchmarkDotNet.Diagnosers.RunMode.None, null!),
        };

        var stage = new EngineJitStage(evaluateOverhead: false, parameters, listener);
        var measurements = stage.GetMeasurementList();
        while (stage.GetShouldRunIteration(measurements, out var data))
        {
            data.setupAction().GetAwaiter().GetResult();
            data.workloadAction(data.invokeCount / data.unrollFactor, null!).GetAwaiter().GetResult();
            data.cleanupAction().GetAwaiter().GetResult();
            // A zero-time measurement keeps the stage out of its "long-running benchmark" early-exit
            // (iterationTime / 0 == Infinity, and Infinity < 1.5 is false).
            measurements.Add(new Measurement(1, data.mode, data.stage, data.index, data.invokeCount, 0d));
        }
    }

    // Benchmark-method stand-ins. A distinct method per tier-up scenario so that one test tiering a method up doesn't
    // affect another's starting state. In a DisableOptimizations build these are JITted at minopts and never tier; in
    // an optimized build they reach OptimizedTier1.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Cold(long x) => x * x + 1;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long AlreadyTier1(long x) => x * x + 1;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long AlreadyTier0(long x) => x * x + 1;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long AlreadyTier0DelayedCallCounting(long x) => x * x + 1;
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static long NoOptimization(long x) => x * x + 1;
    [MethodImpl(MethodImplOptions.NoInlining | CodeGenHelper.AggressiveOptimizationOption)]
    private static long AggressiveOptimization(long x) => x * x + 1;
    // Two distinct methods watched together by one listener (JitStage_MultipleMethods).
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long MultiFirst(long x) => x * x + 1;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long MultiSecond(long x) => x * x + 1;
    // Watched together but invoked at different rates (JitStage_MultipleMethodsUnevenInvocationRates).
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long MultiUnevenFast(long x) => x * x + 1;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long MultiUnevenSlow(long x) => x * x + 1;

    // Generic stand-ins. GenericBench is instantiated over one value type and one reference type by separate tests, so
    // each test drives a distinct instantiation and can't tier another's up. SharedGenericBench is separate from it so
    // that the two-reference-type test starts from cold instantiations of its own.
    private static class GenericBench<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static long Method(long x) => x * x + 1;
    }
    private static class SharedGenericBench<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static long Method(long x) => x * x + 1;
    }
    // Two value-type instantiations, driven against each other by JitStage_DistinctValueTypeInstantiationsAreNotConfused.
    private static class DistinctBench<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static long Method(long x) => x * x + 1;
    }
    // Mixed reference/value arguments (JitStage_GenericTypeWithMixedArguments).
    private static class PairBench<T1, T2>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static long Method(long x) => x * x + 1;
    }
    // Two canonicalized instantiations differing only in their value-type argument, driven against each other by
    // JitStage_DistinctCanonicalizedInstantiationsAreNotConfused.
    private static class DistinctPairBench<T1, T2>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static long Method(long x) => x * x + 1;
    }
    // Never driven — JitListener_ThrowsForGenericMethod only needs its MethodInfo.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long GenericMethod<T>(long x) => x * x + 1;
    // Overload pairs, driven against each other by the *OverloadsAreNotConfused tests. Each pair shares a name and a
    // declaring type and differs only in its parameter type — i.e. only in its metadata token.
    private static class OverloadBench
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static long Method(long x) => x * x + 1;
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static long Method(int x) => x * x + 1;
    }
    private static class OverloadPairBench<T1, T2>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static long Method(long x) => x * x + 1;
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static long Method(int x) => x * x + 1;
    }

    // A loop long enough to cross the OSR back-edge threshold so these methods are On-Stack-Replaced where OSR is
    // enabled. Timing is irrelevant (RunJitStageToCompletion records 0ns measurements, so the stage never takes its
    // long-running early-exit), so the only requirement is enough iterations to trigger OSR.
    private const int OsrLoopCount = 1_000_000;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Osr(long x)
    {
        long sum = x;
        for (int i = 0; i < OsrLoopCount; i++)
            sum += i;
        return sum;
    }
    // The benchmark method: it does nothing but call the OSR'd method, which NoInlining keeps as a separate jit unit.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long CallsOsr(long x) => OsrCallee(x);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long OsrCallee(long x)
    {
        long sum = x;
        for (int i = 0; i < OsrLoopCount; i++)
            sum += i;
        return sum;
    }

    // Minimal host that surfaces a cancellation token so the stage's unbounded per-tier wait stays interruptible.
    private sealed class CancellableHost(CancellationToken cancellationToken) : IHost
    {
        public CancellationToken CancellationToken { get; } = cancellationToken;
        public void Dispose() { }
        public void WriteLine() { }
        public void WriteLine(string message) { }
        public void SendError(string message) { }
        public void ReportResults(RunResults runResults) { }
        public ValueTask SendSignalAsync(HostSignal hostSignal) => new();
        public ValueTask Yield() => new();
    }
}
