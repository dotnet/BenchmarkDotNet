using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Tasks;
using System;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Plugins;
using BenchmarkDotNet.Plugins.Loggers;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 5)]
    public class PerformanceUnitTest
    {
        [Fact]
        public void Test()
        {
            var logger = new BenchmarkAccumulationLogger();
            var plugins = BenchmarkPluginBuilder.CreateDefault().AddLogger(logger).Build();
            var reports = new BenchmarkRunner(plugins).Run<PerformanceUnitTest>();

            // Sanity checks, to be sure that the different benchmarks actually run
            var testOutput = logger.GetLog();
            Assert.Contains("// ### Slow Benchmark called ###" + Environment.NewLine, testOutput);
            Assert.Contains("// ### Fast Benchmark called ###" + Environment.NewLine, testOutput);

            // Check that slow benchmark is actually slower than the fast benchmark!
            var slowBenchmarkRun = reports.GetRunsFor<PerformanceUnitTest>(r => r.SlowBenchmark()).First();
            var fastBenchmarkRun = reports.GetRunsFor<PerformanceUnitTest>(r => r.FastBenchmark()).First();
            Assert.True(slowBenchmarkRun.AverageNanoseconds > fastBenchmarkRun.AverageNanoseconds,
                        string.Format("Expected SlowBenchmark: {0:N2} ns to be MORE than FastBenchmark: {1:N2} ns",
                                      slowBenchmarkRun.AverageNanoseconds, fastBenchmarkRun.AverageNanoseconds));
            Assert.True(slowBenchmarkRun.OpsPerSecond < fastBenchmarkRun.OpsPerSecond,
                        string.Format("Expected SlowBenchmark: {0:N2} Ops to be LESS than FastBenchmark: {1:N2} Ops",
                                      slowBenchmarkRun.OpsPerSecond, fastBenchmarkRun.OpsPerSecond));

            // Whilst we're at it, let's do more specific Asserts as we know what the elasped time should be
            var slowBenchmarkReport = reports.GetReportFor<PerformanceUnitTest>(r => r.SlowBenchmark());
            var fastBenchmarkReport = reports.GetReportFor<PerformanceUnitTest>(r => r.FastBenchmark());
            foreach (var slowRun in slowBenchmarkReport.Runs)
                Assert.InRange(slowRun.AverageNanoseconds / 1000.0 / 1000.0, low: 499, high: 502);
            foreach (var fastRun in fastBenchmarkReport.Runs)
                Assert.InRange(fastRun.AverageNanoseconds / 1000.0 / 1000.0, low: 14, high: 17);
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
            Thread.Sleep(500);
        }
    }
}
