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
    public class JobTests : BenchmarkTestExecutor
    {
        public JobTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void ZeroWarmupCountIsApplied()
        {
            var job = Job.Default
                .WithEvaluateOverhead(false)
                .WithWarmupCount(0)
                .WithIterationCount(1)
                .WithInvocationCount(1)
                .WithUnrollFactor(1);
            var config = DefaultConfig.Instance.AddJob(job).WithOptions(ConfigOptions.DisableOptimizationsValidator);
            var summary = CanExecute<ZeroWarmupBench>(config);
            var report = summary.Reports.Single();
            int workloadWarmupCount = report.AllMeasurements
                .Count(m => m.Is(IterationMode.Workload, IterationStage.Warmup));
            Assert.Equal(0, workloadWarmupCount);
        }

        public class ZeroWarmupBench
        {
            [Benchmark]
            public void Foo() => Thread.Sleep(10);
        }
    }
}