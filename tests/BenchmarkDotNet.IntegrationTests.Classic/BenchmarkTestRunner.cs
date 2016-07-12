using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests.Classic
{
    internal static class BenchmarkTestRunner
    {
        internal static void CanCompileAndRun<TBenchmark>()
        {
            var summary = BenchmarkRunner.Run<TBenchmark>(new SingleRunFastConfig());

            Assert.True(summary.Reports.Any());
            Assert.True(summary.Reports.All(report => report.ExecuteResults.All(executeResult => executeResult.FoundExecutable)));
            Assert.True(summary.Reports.All(report => report.AllMeasurements.Any()));
        }

        private class SingleRunFastConfig : ManualConfig
        {
            internal SingleRunFastConfig()
            {
                Add(Job.Dry);
                Add(BenchmarkDotNet.Loggers.ConsoleLogger.Default);
            }
        }
    }
}