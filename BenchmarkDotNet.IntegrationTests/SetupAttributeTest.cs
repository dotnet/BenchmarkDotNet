using BenchmarkDotNet.Tasks;
using System;
using System.Threading;
using BenchmarkDotNet.Plugins;
using BenchmarkDotNet.Plugins.Loggers;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    public class SetupAttributeTest
    {
        [Fact]
        public void Test()
        {
            var logger = new BenchmarkAccumulationLogger();
            var plugins = BenchmarkPluginBuilder.CreateDefault().AddLogger(logger).Build();
            var reports = new BenchmarkRunner(plugins).Run<SetupAttributeTest>();
            Assert.Contains("// ### Setup called ###" + Environment.NewLine, logger.GetLog());
        }

        [Setup]
        public void Setup()
        {
            Console.WriteLine("// ### Setup called ###");
        }

        [Benchmark]
        [BenchmarkTask(mode: BenchmarkMode.SingleRun, processCount: 1, warmupIterationCount: 1, targetIterationCount: 1)]
        public void Benchmark()
        {
            Console.WriteLine("// ### Benchmark called ###");
            Thread.Sleep(5);
        }
    }
}
