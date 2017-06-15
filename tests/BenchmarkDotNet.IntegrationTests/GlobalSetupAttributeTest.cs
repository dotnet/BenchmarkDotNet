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
    public class GlobalSetupAttributeTest : BenchmarkTestExecutor
    {
        private const string GlobalSetupCalled = "// ### GlobalSetup called ###";
        private const string BenchmarkCalled = "// ### Benchmark called ###";

        public GlobalSetupAttributeTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void GlobalSetupMethodRunsTest()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            CanExecute<GlobalSetupAttributeBenchmarks>(config);

            string log = logger.GetLog();
            Assert.Contains(GlobalSetupCalled + Environment.NewLine, log);
            Assert.True(
                log.IndexOf(GlobalSetupCalled + Environment.NewLine) <
                log.IndexOf(BenchmarkCalled + Environment.NewLine));
        }

        public class GlobalSetupAttributeBenchmarks
        {
            [GlobalSetup]
            public void GlobalSetup()
            {
                Console.WriteLine(GlobalSetupCalled);
            }

            [Benchmark]
            public void Benchmark()
            {
                Console.WriteLine(BenchmarkCalled);
            }
        }
    }
}
