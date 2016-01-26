using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    [Config(typeof(ThroughputFastConfig))]
    public class ModeTests
    {
        [Fact]
        public void Test()
        {
            var logger = new AccumulationLogger();
            var config = DefaultConfig.Instance.With(logger);

            BenchmarkRunner.Run<ModeTests>(config);
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
        public void BenchmarkSingleRunVoid()
        {
            Console.WriteLine("// ### BenchmarkSingleRunVoid method called ###");
        }

        [Benchmark]
        public string BenchmarkSingleRunWithReturnValue()
        {
            Console.WriteLine("// ### BenchmarkSingleRunWithReturnValue method called ###");
            return "okay";
        }

        [Benchmark]
        public void BenchmarkThroughputVoid()
        {
            if (FirstTime)
            {
                Console.WriteLine("// ### BenchmarkThroughputVoid method called ###");
                FirstTime = false;
            }
        }

        [Benchmark]
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
