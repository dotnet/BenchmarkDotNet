using BenchmarkDotNet.Jobs;
using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests
{
    public class SetupAttributeTest : BenchmarkTestExecutor
    {
        public SetupAttributeTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void SetupAttributeMethodGetsCalled()
        {
            var logger = new OutputLogger(Output);
            var config = CreateSimpleConfig(logger);

            CanExecute<SetupAttributeBenchmarks>(config);
            Assert.Contains("// ### Setup called ###" + Environment.NewLine, logger.GetLog());
        }

        public class SetupAttributeBenchmarks
        {
            [Setup]
            public void Setup()
            {
                Console.WriteLine("// ### Setup called ###");
            }

            [Benchmark]
            public void Benchmark()
            {
                Console.WriteLine("// ### Benchmark called ###");
                Thread.Sleep(5);
            }
        }
    }
}
