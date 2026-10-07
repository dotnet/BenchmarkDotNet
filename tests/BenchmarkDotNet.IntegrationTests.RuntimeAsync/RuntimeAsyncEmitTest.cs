using BenchmarkDotNet.Configs;
using BenchmarkDotNet.IntegrationTests.InProcess.EmitTests;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tests.Loggers;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using System.Reflection;

namespace BenchmarkDotNet.IntegrationTests.RuntimeAsync;

public class RuntimeAsyncEmitTest(ITestOutputHelper output) : BenchmarkTestExecutor(output)
{
    private const MethodImplAttributes AsyncMethodImpl = (MethodImplAttributes)0x2000;

    [Theory]
    [InlineData(typeof(RuntimeAsyncTaskCaseBenchmark))]
    [InlineData(typeof(RuntimeAsyncSetupCleanupBenchmark))]
    [InlineData(typeof(RuntimeAsyncBuilderOverrideBenchmark))]
    public void InProcessBenchmarkEmitsSameIL(Type benchmarkType)
    {
        // Without runtime-async compiled methods, both sides would be state machines and the diff would prove nothing.
        var asyncMethods = benchmarkType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m.ReturnType != typeof(void))
            .ToArray();
        Assert.NotEmpty(asyncMethods);
        Assert.All(asyncMethods, m => Assert.True(m.MethodImplementationFlags.HasFlag(AsyncMethodImpl), $"{m.Name} is not runtime-async"));

        var logger = new OutputLogger(Output);
        var config = new ManualConfig()
            .AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default).WithInvocationCount(4).WithUnrollFactor(4))
            .AddJob(Job.Dry.WithInvocationCount(4).WithUnrollFactor(4))
            .WithOptions(ConfigOptions.KeepBenchmarkFiles)
            .AddLogger(logger);

        var summary = CanExecute(benchmarkType, config);

        DiffEmit(summary);
    }

    // Without KeepBenchmarkFiles the runnables are emitted into a collectible assembly instead of a persisted one.
    [Fact]
    public void InProcessBenchmarkRunsWithoutSaving()
    {
        var config = new ManualConfig()
            .AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.Default).WithInvocationCount(4).WithUnrollFactor(4))
            .AddLogger(new OutputLogger(Output));

        CanExecute<RuntimeAsyncTaskCaseBenchmark>(config);
    }

    private static void DiffEmit(Summary summary)
    {
        var emittedReport = summary.Reports.First(r => r.BenchmarkCase.GetToolchain() is InProcessEmitToolchain);
        var compiledReport = summary.Reports.First(r => r.BenchmarkCase.GetToolchain() is not InProcessEmitToolchain);
        NaiveRunnableEmitDiff.RunDiff(
            compiledReport.BuildResult.ArtifactsPaths.ExecutablePath,
            emittedReport.BuildResult.ArtifactsPaths.ExecutablePath,
            ConsoleLogger.Default);
    }
}
