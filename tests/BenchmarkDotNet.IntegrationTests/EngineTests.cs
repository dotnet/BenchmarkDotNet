using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using Xunit;
using Xunit.Abstractions;

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

            Check(IterationMode.Overhead, IterationStage.Jitting);
            Check(IterationMode.Workload, IterationStage.Jitting);
            Check(IterationMode.Overhead, IterationStage.Warmup);
            Check(IterationMode.Overhead, IterationStage.Actual);
            Check(IterationMode.Workload, IterationStage.Warmup);
            Check(IterationMode.Workload, IterationStage.Actual);
            Check(IterationMode.Workload, IterationStage.Result);
        }

        public class FooBench
        {
            [Benchmark]
            public void Foo() => Thread.Sleep(10);
        }
    }
}