using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class GlobalCleanupAttributeTest : BenchmarkTestExecutor
    {
        private const string GlobalCleanupCalled = "// ### GlobalCleanup called ###";
        private const string BenchmarkCalled = "// ### Benchmark called ###";

        public GlobalCleanupAttributeTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void GlobalCleanupMethodRunsTest()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            var summary = CanExecute<GlobalCleanupAttributeBenchmarks>(config);

            string[] expected = new string[]
            {
                BenchmarkCalled,
                GlobalCleanupCalled
            };

            Assert.Equal(expected, GetSingleStandardOutput(summary));
        }

        public class GlobalCleanupAttributeBenchmarks
        {
            [Benchmark]
            public void Benchmark()
            {
                Console.WriteLine(BenchmarkCalled);
            }

            [GlobalCleanup]
            public void GlobalCleanup()
            {
                Console.WriteLine(GlobalCleanupCalled);
            }
        }
    }
}
