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

        using var observer = JitListener.Create(workloadMethod.Method);

        RunJitStageToCompletion(workloadMethod, observer);

        AssertReachedFinalTier(observer);
    }

    // Tests the case of InProcess benchmarking the same method multiple times.
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_AlreadyTier1()
    {
        Func<long, long> workloadMethod = AlreadyTier1;

        using var observer = JitListener.Create(workloadMethod.Method);

        // The first jit stage brings the method to tier1 (in an optimized build) and our observer records it. Running
        // the jit stage again for the same (now tier1) method should also succeed; it gets a fresh listener, because
        // the stage drove the first to completion and reusing one across runs would leave its tiering signals ambiguous.
        RunJitStageToCompletion(workloadMethod, observer);
        using var observer2 = JitListener.Create(workloadMethod.Method);
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
        using var observer = JitListener.Create(workloadMethod.Method);

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
        using var observer = JitListener.Create(workloadMethod.Method);

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

        using var observer = JitListener.Create(workloadMethod.Method);

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

        using var observer = JitListener.Create(workloadMethod.Method);
        // The stage only drives (and the engine's listener only watches) the benchmark method, but every call to it
        // calls the OSR'd callee, so the callee should be driven all the way to tier1 too. Watch it independently.
        using var calleeObserver = JitListener.Create(calleeMethod.Method);

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

        using var observer = JitListener.Create(workloadMethod.Method);

        RunJitStageToCompletion(workloadMethod, observer);

        AssertReachedFinalTier(observer);
    }

    // [MethodImpl(AggressiveOptimization)] pins the method straight to optimized code, so it never goes through
    // tier0 -> tier1 and its final tier is Optimized (or OptimizedTier1, depending on runtime).
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_AggressiveOptimization()
    {
        Func<long, long> workloadMethod = AggressiveOptimization;

        using var observer = JitListener.Create(workloadMethod.Method);

        RunJitStageToCompletion(workloadMethod, observer);

        AssertReachedFinalTier(observer);
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
    {
        // The per-tier publication wait is unbounded, cancellable only via the host's token. When the method tiers, the
        // stage re-bursts until the runtime reports the next-tier compile began, so the wait isn't actually hit — but a
        // large timeout guards against a hang if tiering somehow stalls, instead of wedging the whole test run.
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var host = new CancellableHost(timeout.Token);
        Func<long, IClock, ValueTask<ClockSpan>> empty = (_, _) => new(default(ClockSpan));
        Func<long, IClock, ValueTask<ClockSpan>> workload = (invokeCount, _) =>
        {
            // Really invoke the benchmark method so it goes through call counting and tiers up for real.
            for (long i = 0; i < invokeCount; i++)
                workloadMethod(i);
            return new(default(ClockSpan));
        };

        var parameters = new EngineParameters
        {
            Host = host,
            WorkloadMethod = workloadMethod.Method,
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
