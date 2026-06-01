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
    // The jit stage's behavior depends on whether the benchmark method's assembly is optimized, and this test
    // assembly is built both ways across configurations, so each case asserts whichever applies:
    //   * Optimized build: the target method participates in tiered compilation, so the stage drives it to
    //     OptimizedTier1. We verify through a SECOND, independent JitListener created before the stage runs —
    //     multiple EventListeners each receive the same runtime events, so it observes exactly what the stage's
    //     internal listener does, and unlike the internal listener (disposed when the stage ends) it outlives the stage.
    //   * DisableOptimizations build (e.g. Debug): the assembly is JITted at minopts and never tiers, so the listener
    //     declines to watch it (Create returns null) and the stage falls back to the fixed-delay loop.
    private static readonly bool OptimizationsDisabled =
        typeof(JitListenerTests).Assembly.GetCustomAttribute<System.Diagnostics.DebuggableAttribute>()?.IsJITOptimizerDisabled ?? false;

    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_Cold()
    {
        Func<long, long> workloadMethod = Cold;

        using var observer = JitListener.Create(workloadMethod.Method, enabled: true);

        RunJitStageToCompletion(workloadMethod);

        AssertTierUpOrDeclined(observer);
    }

    // Tests the case of InProcess benchmarking the same method multiple times.
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_AlreadyTier1()
    {
        Func<long, long> workloadMethod = AlreadyTier1;

        using var observer = JitListener.Create(workloadMethod.Method, enabled: true);

        // The first jit stage brings the method to tier1 (in an optimized build); running the jit stage again for the
        // same method should succeed without issue and leave the method in tier1.
        RunJitStageToCompletion(workloadMethod);
        RunJitStageToCompletion(workloadMethod);

        AssertTierUpOrDeclined(observer);
    }

    // Tests the case of InProcess benchmarking a method that the user already invoked before starting the benchmarks when call counting is active.
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_AlreadyTier0()
    {
        DeadCodeEliminationHelper.KeepAliveWithoutBoxing(AlreadyTier0(42));
        // Sleep long enough for the tiered call counting to begin.
        Engine.SleepIfPositive(JitInfo.TieredDelay + JitInfo.TieredDelay);
        Func<long, long> workloadMethod = AlreadyTier0;

        using var observer = JitListener.Create(workloadMethod.Method, enabled: true);

        RunJitStageToCompletion(workloadMethod);

        AssertTierUpOrDeclined(observer);
    }

    // Tests the case of InProcess benchmarking a method that the user already invoked before starting the benchmarks when call counting is delayed.
    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void JitStage_AlreadyTier0DelayedCallCounting()
    {
        DeadCodeEliminationHelper.KeepAliveWithoutBoxing(AlreadyTier0DelayedCallCounting(42));
        Func<long, long> workloadMethod = AlreadyTier0DelayedCallCounting;

        using var observer = JitListener.Create(workloadMethod.Method, enabled: true);

        RunJitStageToCompletion(workloadMethod);

        AssertTierUpOrDeclined(observer);
    }

    // A pinned optimization level makes a method ineligible for tiered compilation regardless of the assembly, so the
    // listener declines to watch it (Create returns null). In an optimized build that attribute is the sole reason; in a
    // DisableOptimizations build the assembly excludes it too — either way there is nothing for the listener to observe.

    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void Create_DeclinesNoOptimizationMethod()
    {
        // [MethodImpl(NoOptimization)] pins the method to minopts, so it never tiers.
        Func<long, long> workloadMethod = NoOptimization;
        using var listener = JitListener.Create(workloadMethod.Method, enabled: true);
        Assert.Null(listener);
    }

    [FactEnvSpecific("Only CoreCLR supports tiered JIT", EnvRequirement.DotNetCoreOnly)]
    public void Create_DeclinesAggressiveOptimizationMethod()
    {
        // [MethodImpl(AggressiveOptimization)] pins the method straight to tier1, so it never goes through tier0 -> tier1.
        Func<long, long> workloadMethod = AggressiveOptimization;
        using var listener = JitListener.Create(workloadMethod.Method, enabled: true);
        Assert.Null(listener);
    }

    private static void AssertTierUpOrDeclined(JitListener? observer)
    {
        if (OptimizationsDisabled)
        {
            // The method can't tier, so the listener declines to watch it (Create returns null) and the stage falls
            // back to the fixed-delay loop without ever reaching tier1.
            Assert.Null(observer);
        }
        else
        {
            // requires EventSource support in the test host (it's enabled by default)
            Assert.NotNull(observer);
            Assert.True(observer!.ReachedTier1, "the jit stage should have driven the benchmark method to tier1");
        }
    }

    private static void RunJitStageToCompletion(Func<long, long> workloadMethod)
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

        var stage = new EngineJitStage(evaluateOverhead: false, parameters);
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
