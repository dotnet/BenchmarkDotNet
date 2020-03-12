using System;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class PerformanceUnitTestRunner
    {
        private readonly ITestOutputHelper output;

        public PerformanceUnitTestRunner(ITestOutputHelper outputHelper)
        {
            output = outputHelper;
        }

        // See also: https://github.com/dotnet/BenchmarkDotNet/issues/204
        [Fact(Skip = "This test fails on AppVeyor")]
        public void Test()
        {
            var logger = new OutputLogger(output);
            var config = DefaultConfig.Instance.AddLogger(logger);
            var summary = BenchmarkRunner.Run<PerformanceUnitTest>(config);

            // Sanity checks, to be sure that the different benchmarks actually run
            var testOutput = logger.GetLog();
            Assert.Contains("// ### Slow Benchmark called ###" + Environment.NewLine, testOutput);
            Assert.Contains("// ### Fast Benchmark called ###" + Environment.NewLine, testOutput);

            // Check that slow benchmark is actually slower than the fast benchmark!
            var slowBenchmarkRun = summary.GetRunsFor<PerformanceUnitTest>(r => r.SlowBenchmark()).First();
            var fastBenchmarkRun = summary.GetRunsFor<PerformanceUnitTest>(r => r.FastBenchmark()).First();
            Assert.True(slowBenchmarkRun.GetAverageTime() > fastBenchmarkRun.GetAverageTime(),
                        string.Format("Expected SlowBenchmark: {0:N2} ns to be MORE than FastBenchmark: {1:N2} ns",
                                      slowBenchmarkRun.GetAverageTime().Nanoseconds, fastBenchmarkRun.GetAverageTime().Nanoseconds));
            Assert.True(slowBenchmarkRun.GetOpsPerSecond() < fastBenchmarkRun.GetOpsPerSecond(),
                        string.Format("Expected SlowBenchmark: {0:N2} Ops to be LESS than FastBenchmark: {1:N2} Ops",
                                      slowBenchmarkRun.GetOpsPerSecond(), fastBenchmarkRun.GetOpsPerSecond()));

            // Whilst we're at it, let's do more specific Asserts as we know what the elapsed time should be
            var slowBenchmarkReport = summary.GetReportFor<PerformanceUnitTest>(r => r.SlowBenchmark());
            var fastBenchmarkReport = summary.GetReportFor<PerformanceUnitTest>(r => r.FastBenchmark());
            foreach (var slowRun in slowBenchmarkReport.GetResultRuns())
                Assert.InRange(slowRun.GetAverageTime().Nanoseconds / 1000.0 / 1000.0, low: 98, high: 102);
            foreach (var fastRun in fastBenchmarkReport.GetResultRuns())
                Assert.InRange(fastRun.GetAverageTime().Nanoseconds / 1000.0 / 1000.0, low: 14, high: 17);
        }
    }

    [Config(typeof(SingleRunFastConfig))]
    public class PerformanceUnitTest
    {
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
