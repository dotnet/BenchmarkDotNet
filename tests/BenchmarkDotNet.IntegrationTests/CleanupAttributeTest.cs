using BenchmarkDotNet.Jobs;
using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class CleanupAttributeTest : BenchmarkTestExecutor
    {
        private const string CleanupCalled = "// ### Cleanup called ###";
        private const string BenchmarkCalled = "// ### Benchmark called ###";

        public CleanupAttributeTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void CleanupMethodRunsTest()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            CanExecute<CleanupAttributeBenchmarks>(config);

            string log = logger.GetLog();
            Assert.Contains(CleanupCalled + Environment.NewLine, log);
            Assert.True(
                log.IndexOf(CleanupCalled + Environment.NewLine) > 
                log.IndexOf(BenchmarkCalled + Environment.NewLine));
        }

        public class CleanupAttributeBenchmarks
        {
            [Benchmark]
            public void Benchmark()
            {
                Console.WriteLine(BenchmarkCalled);
            }

            [Cleanup]
            public void Cleanup()
            {
                Console.WriteLine(CleanupCalled);
            }
        }
    }
}
