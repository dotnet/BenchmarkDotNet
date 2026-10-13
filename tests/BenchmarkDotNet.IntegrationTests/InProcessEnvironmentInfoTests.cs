using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.IntegrationTests.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using DiagnoserRunMode = BenchmarkDotNet.Diagnosers.RunMode;

namespace BenchmarkDotNet.IntegrationTests;

public class InProcessEnvironmentInfoTests(ITestOutputHelper output) : BenchmarkTestExecutor(output)
{
    public static TheoryData<IToolchain> GetToolchains() =>
    [
        InProcessEmitToolchain.Default,
        InProcessNoEmitToolchain.Default
    ];

    [Theory]
    [MemberData(nameof(GetToolchains), DisableDiscoveryEnumeration = true)]
    public void EnvironmentInfoIsIncludedInReport(IToolchain toolchain)
    {
        try
        {
            var diagnoser = new MockInProcessDiagnoser1(DiagnoserRunMode.NoOverhead);
            var config = new ManualConfig()
                .AddJob(Job.Dry.WithToolchain(toolchain))
                .AddDiagnoser(diagnoser);
            var summary = CanExecute<EnvironmentInfoBenchmark>(config);

            var report = Assert.Single(summary.Reports);
            Assert.False(string.IsNullOrWhiteSpace(report.GetRuntimeInfo()));
            Assert.False(string.IsNullOrWhiteSpace(report.GetGcInfo()));
            Assert.NotNull(report.GetHardwareIntrinsicsInfo());

            var executeResult = Assert.Single(report.ExecuteResults);
            Assert.Contains(executeResult.PrefixedLines, line => line.StartsWith("// Runtime=", StringComparison.Ordinal));
            Assert.Contains(executeResult.PrefixedLines, line => line.StartsWith("// GC=", StringComparison.Ordinal));
            Assert.Contains(executeResult.PrefixedLines, line => line.StartsWith("// HardwareIntrinsics=", StringComparison.Ordinal));
            Assert.DoesNotContain(executeResult.PrefixedLines, line => line.StartsWith("// InProcessDiagnoser", StringComparison.Ordinal));
            Assert.Equal(diagnoser.ExpectedResult, diagnoser.Results[report.BenchmarkCase]);
        }
        finally
        {
            BaseMockInProcessDiagnoser.s_completedResults.Clear();
        }
    }

    public class EnvironmentInfoBenchmark
    {
        [Benchmark]
        public void Run()
        {
        }
    }
}
