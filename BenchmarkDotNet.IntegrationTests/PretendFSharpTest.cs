using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    // The referenced file "FSharpBenchmarkDotNet.exe" is compiled from F# code as shown in the file "PretendFSharpTest.fs.
    // That .fs file is NOT meant to be compiled as part of our Integration tests, it is just there for reference.
    // (It seemed simpler to do it this way, rather than trying to compile F# code as part of our integration tests)
    public class PretendFSharpTest
    {
        [Fact]
        public void Test()
        {
            var logger = new AccumulationLogger();
            var config = DefaultConfig.Instance.With(logger);
            // Run our "Pretend F# test" (see above for more info)
            BenchmarkRunner.Run<BenchmarkSpec.Db>(config);
            var testLog = logger.GetLog();
            Assert.Contains("// ### F# Benchmark method called ###", testLog);
            Assert.DoesNotContain("No benchmarks found", testLog); // TODO: move message to const for all of the test
        }
    }
}
