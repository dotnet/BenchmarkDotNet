using BenchmarkDotNet.Jobs;
using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    [Config(typeof(SingleRunFastConfig))]
    public class SetupAttributeTest
    {
        [Fact]
        public void Test()
        {
            var logger = new AccumulationLogger();
            var config = DefaultConfig.Instance.With(logger);
            BenchmarkTestExecutor.CanExecute<SetupAttributeTest>(config);
            Assert.Contains("// ### Setup called ###" + Environment.NewLine, logger.GetLog());
        }

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
