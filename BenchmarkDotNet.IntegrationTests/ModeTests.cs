using System;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class ModeTests
    {
        private static AccumulationLogger logger = new AccumulationLogger();

        public class ModeConfig : ManualConfig
        {
            public ModeConfig()
            {
                // Use our own Config, as we explicitly want to test the different "Mode" values, i.e. SingleRun and Throughput
                Add(Job.Dry.With(Mode.SingleRun));
                Add(Job.Dry.With(Mode.Throughput));
                Add(DefaultConfig.Instance.GetLoggers().ToArray());
                Add(logger); // So we can capture/parse the output in our test
                Add(DefaultConfig.Instance.GetColumns().ToArray());
            }
        }

        [Fact]
        public void Test()
        {
            logger.ClearLog();
            var results = BenchmarkTestExecutor.CanExecute<ModeTests>(new ModeConfig());

            Assert.Equal(4, results.Benchmarks.Count());

            Assert.Equal(1, results.Benchmarks.Count(b => b.Job.Mode == Mode.SingleRun && b.Target.Method.Name == "BenchmarkWithVoid"));
            Assert.Equal(1, results.Benchmarks.Count(b => b.Job.Mode == Mode.SingleRun && b.Target.Method.Name == "BenchmarkWithReturnValue"));

            Assert.Equal(1, results.Benchmarks.Count(b => b.Job.Mode == Mode.Throughput && b.Target.Method.Name == "BenchmarkWithVoid"));
            Assert.Equal(1, results.Benchmarks.Count(b => b.Job.Mode == Mode.Throughput && b.Target.Method.Name == "BenchmarkWithReturnValue"));

            var testLog = logger.GetLog();
            Assert.Contains("// ### Benchmark with void called ###", testLog);
            Assert.Contains("// ### Benchmark with return value called ###", testLog);
            Assert.DoesNotContain("No benchmarks found", logger.GetLog());
        }

        public bool FirstTime { get; set; }

        [Setup]
        public void Setup()
        {
            // Ensure we only print the diagnostic messages once per run in the tests, otherwise it fills up the output log!!
            FirstTime = true;
        }

        [Benchmark]
        public void BenchmarkWithVoid()
        {
            Thread.Sleep(10);
            if (FirstTime)
            {
                Console.WriteLine("// ### Benchmark with void called ###");
                FirstTime = false;
            }
        }

        [Benchmark]
        public string BenchmarkWithReturnValue()
        {
            Thread.Sleep(10);
            if (FirstTime)
            {
                Console.WriteLine("// ### Benchmark with return value called ###");
                FirstTime = false;
            }
            return "okay";
        }
    }
}
