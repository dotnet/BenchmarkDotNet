using BenchmarkDotNet.Tasks;
using System;
using System.Linq;
using System.Threading;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
    public class PerformanceUnitTest : IntegrationTestBase
    {
        [Fact]
        public void Test()
        {
            var reports = new BenchmarkRunner().Run<PerformanceUnitTest>();

            // Sanity checks, to be sure that the different benchmarks actually run
            var testOutput = GetTestOutput();
            Assert.Contains("// ### Slow Benchmark called ###" + Environment.NewLine, testOutput);
            Assert.Contains("// ### Fast Benchmark called ###" + Environment.NewLine, testOutput);

            // TODO Wrap this up in some helper methods, so it's easier to get the reports/runs for a given method
            var slowBenchmarkReport = reports.First(r => r.Benchmark.Target.Method.Name == "SlowBenchmark");
            var slowBenchmarkRun = slowBenchmarkReport.Runs.First();
            var fastBenchmarkReport = reports.First(r => r.Benchmark.Target.Method.Name == "FastBenchmark");
            var fastBenchmarkRun = fastBenchmarkReport.Runs.First();

            // Check that slow benchmark is actually slower than the fast benchmark!
            Assert.True(slowBenchmarkRun.AverageNanoseconds > fastBenchmarkRun.AverageNanoseconds,
                        string.Format("Expected SlowBenchmark: {0:N2} ns to be MORE than FastBenchmark: {1:N2} ns",
                                      slowBenchmarkRun.AverageNanoseconds, fastBenchmarkRun.AverageNanoseconds));
            Assert.True(slowBenchmarkRun.OpsPerSecond < fastBenchmarkRun.OpsPerSecond,
                        string.Format("Expected SlowBenchmark: {0:N2} Ops to be LESS than FastBenchmark: {1:N2} Ops",
                                      slowBenchmarkRun.OpsPerSecond, fastBenchmarkRun.OpsPerSecond));

            // Whilst we're at it, let's do more specific Asserts as we know what the elasped time should be
            Assert.InRange(slowBenchmarkRun.AverageNanoseconds / 1000.0 / 1000.0, low: 499, high: 501);
            Assert.InRange(fastBenchmarkRun.AverageNanoseconds / 1000.0 / 1000.0, low: 4.5, high: 5.5);
        }

        [Benchmark]
        public void FastBenchmark()
        {
            Console.WriteLine("// ### Fast Benchmark called ###");
            Thread.Sleep(5);
        }

        [Benchmark]
        public void SlowBenchmark()
        {
            Console.WriteLine("// ### Slow Benchmark called ###");
            Thread.Sleep(500);
        }
    }
}
