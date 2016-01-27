using System;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    [Config(typeof(SingleRunFastConfig))]
    public class PerformanceUnitTest
    {
        [Fact]
        public void Test()
        {
            var logger = new AccumulationLogger();
            var config = DefaultConfig.Instance.With(logger);
            var summary = BenchmarkRunner.Run<PerformanceUnitTest>(config);

            // Sanity checks, to be sure that the different benchmarks actually run
            var testOutput = logger.GetLog();
            Assert.Contains("// ### Slow Benchmark called ###" + Environment.NewLine, testOutput);
            Assert.Contains("// ### Fast Benchmark called ###" + Environment.NewLine, testOutput);

            // Check that slow benchmark is actually slower than the fast benchmark!
            var slowBenchmarkRun = summary.GetRunsFor<PerformanceUnitTest>(r => r.SlowBenchmark()).First();
            var fastBenchmarkRun = summary.GetRunsFor<PerformanceUnitTest>(r => r.FastBenchmark()).First();
            Assert.True(slowBenchmarkRun.GetAverageNanoseconds() > fastBenchmarkRun.GetAverageNanoseconds(),
                        string.Format("Expected SlowBenchmark: {0:N2} ns to be MORE than FastBenchmark: {1:N2} ns",
                                      slowBenchmarkRun.GetAverageNanoseconds(), fastBenchmarkRun.GetAverageNanoseconds()));
            Assert.True(slowBenchmarkRun.GetOpsPerSecond() < fastBenchmarkRun.GetOpsPerSecond(),
                        string.Format("Expected SlowBenchmark: {0:N2} Ops to be LESS than FastBenchmark: {1:N2} Ops",
                                      slowBenchmarkRun.GetOpsPerSecond(), fastBenchmarkRun.GetOpsPerSecond()));

            // Whilst we're at it, let's do more specific Asserts as we know what the elasped time should be
            var slowBenchmarkReport = summary.GetReportFor<PerformanceUnitTest>(r => r.SlowBenchmark());
            var fastBenchmarkReport = summary.GetReportFor<PerformanceUnitTest>(r => r.FastBenchmark());
            foreach (var slowRun in slowBenchmarkReport.GetTargetRuns())
                Assert.InRange(slowRun.GetAverageNanoseconds() / 1000.0 / 1000.0, low: 98, high: 102);
            foreach (var fastRun in fastBenchmarkReport.GetTargetRuns())
                Assert.InRange(fastRun.GetAverageNanoseconds() / 1000.0 / 1000.0, low: 14, high: 17);
        }

        [Benchmark]
        public void FastBenchmark()
        {
            Console.WriteLine("// ### Fast Benchmark called ###");
            Thread.Sleep(15);
        }

        [Benchmark]
        public void SlowBenchmark()
        {
            Console.WriteLine("// ### Slow Benchmark called ###");
            Thread.Sleep(100);
        }
    }
}
