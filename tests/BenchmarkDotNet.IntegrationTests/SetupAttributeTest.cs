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
    public class SetupAttributeTest : BenchmarkTestExecutor
    {
        private const string SetupCalled = "// ### Setup called ###";
        private const string BenchmarkCalled = "// ### Benchmark called ###";

        public SetupAttributeTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void SetupMethodRunsTest()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            CanExecute<SetupAttributeBenchmarks>(config);

            string log = logger.GetLog();
            Assert.Contains(SetupCalled + Environment.NewLine, log);
            Assert.True(
                log.IndexOf(SetupCalled + Environment.NewLine) <
                log.IndexOf(BenchmarkCalled + Environment.NewLine));
        }

        public class SetupAttributeBenchmarks
        {
            [Setup]
            public void Setup()
            {
                Console.WriteLine(SetupCalled);
            }

            [Benchmark]
            public void Benchmark()
            {
                Console.WriteLine(BenchmarkCalled);
            }
        }
    }
}
