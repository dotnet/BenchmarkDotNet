using BenchmarkDotNet.Plugins;
using BenchmarkDotNet.Plugins.Loggers;
using BenchmarkDotNet.Tasks;
using System;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class BenchmarkModeTests
    {
        [Fact]
        public void Test()
        {
            var logger = new BenchmarkAccumulationLogger();
            var plugins = BenchmarkPluginBuilder.CreateDefault().AddLogger(logger).Build();
            var reports = new BenchmarkRunner(plugins).Run<BenchmarkModeTests>();
            var testLog = logger.GetLog();

            Assert.Contains("// ### BenchmarkSingleRunVoid method called ###", testLog);
            Assert.Contains("// ### BenchmarkSingleRunWithReturnValue method called ###", testLog);

            Assert.Contains("// ### BenchmarkSingleRunVoid method called ###", testLog);
            Assert.Contains("// ### BenchmarkSingleRunWithReturnValue method called ###", testLog);

            Assert.DoesNotContain("No benchmarks found", logger.GetLog());
        }

        public bool FirstTime { get; set; }

        [Setup]
        public void Setup()
        {
            // Ensure we only print the diagnostic messages once per run in Throughput tests, otherwise it fills up the output log!!
            FirstTime = true;
        }

        // Benchmarks using BenchmarkMode.SingleRun (void and returning a value)

        [Benchmark]
        [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public void BenchmarkSingleRunVoid()
        {
            Console.WriteLine("// ### BenchmarkSingleRunVoid method called ###");
        }

        [Benchmark]
        [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public string BenchmarkSingleRunWithReturnValue()
        {
            Console.WriteLine("// ### BenchmarkSingleRunWithReturnValue method called ###");
            return "okay";
        }

        // Benchmarks using BenchmarkMode.Throughput (void and returning a value)

        [Benchmark]
        [BenchmarkTask(mode: BenchmarkMode.Throughput, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public void BenchmarkThroughputVoid()
        {
            if (FirstTime)
            {
                Console.WriteLine("// ### BenchmarkThroughputVoid method called ###");
                FirstTime = false;
            }
        }

        [Benchmark]
        [BenchmarkTask(mode: BenchmarkMode.Throughput, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public string BenchmarkThroughputWithReturnValue()
        {
            if (FirstTime)
            {
                Console.WriteLine("// ### BenchmarkThroughputWithReturnValue method called ###");
                FirstTime = false;
            }
            return "okay";
        }
    }
}
