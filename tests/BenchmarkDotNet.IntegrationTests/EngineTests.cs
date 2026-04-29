using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using System.Diagnostics;

namespace BenchmarkDotNet.IntegrationTests
{
    public class EngineTests : BenchmarkTestExecutor
    {
        public EngineTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ZeroWarmupCountIsApplied()
        {
            var job = Job.InProcess
                .WithEvaluateOverhead(false)
                .WithWarmupCount(0)
                .WithIterationCount(1)
                .WithInvocationCount(1)
                .WithUnrollFactor(1);
            var config = DefaultConfig.Instance.AddJob(job).WithOptions(ConfigOptions.DisableOptimizationsValidator);
            var summary = CanExecute<FooBench>(config);
            var report = summary.Reports.Single();
            int workloadWarmupCount = report.AllMeasurements
                .Count(m => m.Is(IterationMode.Workload, IterationStage.Warmup));
            Assert.Equal(0, workloadWarmupCount);
        }

        [Fact]
        public void AllMeasurementsArePerformedDefault() => AllMeasurementsArePerformed(Job.Default);

        [Fact]
        public void AllMeasurementsArePerformedInProcess() => AllMeasurementsArePerformed(Job.InProcess);

        private void AllMeasurementsArePerformed(Job baseJob)
        {
            var job = baseJob
                .WithWarmupCount(1)
                .WithIterationCount(1)
                .WithInvocationCount(1)
                .WithUnrollFactor(1);
            var config = DefaultConfig.Instance.AddJob(job).WithOptions(ConfigOptions.DisableOptimizationsValidator);
            var summary = CanExecute<FooBench>(config);
            var measurements = summary.Reports.Single().AllMeasurements;

            Output.WriteLine("*** AllMeasurements ***");
            foreach (var measurement in measurements)
                Output.WriteLine(measurement.ToString());
            Output.WriteLine("-----");

            void Check(IterationMode mode, IterationStage stage)
            {
                int count = measurements.Count(m => m.Is(mode, stage));
                Output.WriteLine($"Count({mode}{stage}) = {count}");
                Assert.True(count > 0, $"AllMeasurements don't contain {mode}{stage}");
            }

            Check(IterationMode.Workload, IterationStage.Jitting);
            Check(IterationMode.Workload, IterationStage.Warmup);
            Check(IterationMode.Workload, IterationStage.Actual);
            Check(IterationMode.Workload, IterationStage.Result);
        }

        public class FooBench
        {
            [Benchmark]
            public void Foo() => Thread.Sleep(10);
        }

        public static TheoryData<IToolchain> GetToolchains() => new(
        [
            InProcessEmitToolchain.Default,
            InProcessNoEmitToolchain.Default,
            Job.Default.GetToolchain()
        ]);

        // #1120
        [Theory]
        [MemberData(nameof(GetToolchains))]
        public void BenchmarkIsInvokedWithSameStack(IToolchain toolchain)
        {
            CanExecute<StackBench>(DefaultConfig.Instance
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .AddJob(Job.Default.WithToolchain(toolchain).WithUnrollFactor(1)));
        }

        public class StackBench
        {
            private string? _stacktrace;

            [Benchmark]
            public void CheckStack()
            {
                var stacktrace = new StackTrace(true).ToString();
                if (_stacktrace != null && _stacktrace != stacktrace)
                {
                    throw new Exception($"Stack trace is different between benchmark invocations!\n\n{_stacktrace}\n\n{stacktrace}");
                }
                _stacktrace = stacktrace;
            }
        }
    }
}