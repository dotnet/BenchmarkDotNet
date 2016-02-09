using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class FSharpTest
    {
        [Fact]
        public void Test()
        {
            var logger = new AccumulationLogger();
            var config = DefaultConfig.Instance.With(logger);
            // Run our F# test that lives in "BenchmarkDotNet.IntegrationTests.FSharp"
            BenchmarkRunner.Run<FSharpBenchmark.Db>(config);
            var testLog = logger.GetLog();
            Assert.Contains("// ### F# Benchmark method called ###", testLog);
            Assert.DoesNotContain("No benchmarks found", testLog); // TODO: move message to const for all of the test
        }
    }
}
