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
    public class CleanupAttributeTest
    {
        private const string CleanupCalled = "// ### Cleanup called ###";
        private const string BenchmarkCalled = "// ### Benchmark called ###";

        [Fact]
        public void CleanupMethodRunsTest()
        {
            var logger = new AccumulationLogger();
            var config = DefaultConfig.Instance.With(logger);
            BenchmarkTestExecutor.CanExecute<CleanupAttributeTest>(config);

            string log = logger.GetLog();
            Assert.Contains(CleanupCalled + Environment.NewLine, log);
            Assert.True(
                log.IndexOf(CleanupCalled + Environment.NewLine) > 
                log.IndexOf(BenchmarkCalled + Environment.NewLine));
        }

        [Benchmark]
        public void Benchmark()
        {
            Console.WriteLine(BenchmarkCalled);
            Thread.Sleep(5);
        }

        [Cleanup]
        public void Cleanup()
        {
            Console.WriteLine(CleanupCalled);
        }
    }
}
