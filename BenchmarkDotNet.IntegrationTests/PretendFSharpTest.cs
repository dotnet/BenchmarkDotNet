using BenchmarkDotNet.Plugins;
using BenchmarkDotNet.Plugins.Loggers;
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
            var logger = new BenchmarkAccumulationLogger();
            var plugins = BenchmarkPluginBuilder.CreateDefault().AddLogger(logger).Build();
            // Run our "Pretend F# test" (see above for more info)
            var reports = new BenchmarkRunner(plugins).Run<BenchmarkSpec.Db>();
            var testLog = logger.GetLog();
            Assert.Contains("// ### F# Benchmark method called ###", testLog);
            Assert.DoesNotContain("No benchmarks found", testLog);
        }
    }
}
