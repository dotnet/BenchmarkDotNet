using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Tests.Loggers;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.IntegrationTests.Classic
{
    internal static class BenchmarkTestRunner
    {
        internal static void CanCompileAndRun<TBenchmark>(ITestOutputHelper output)
        {
            var summary = BenchmarkRunner.Run<TBenchmark>(new SingleRunFastConfig(output));

            Assert.True(summary.Reports.Any());
            Assert.True(summary.Reports.All(report => report.ExecuteResults.All(executeResult => executeResult.FoundExecutable)));
            Assert.True(summary.Reports.All(report => report.AllMeasurements.Any()), "There are no available measurements");
        }

        private class SingleRunFastConfig : ManualConfig
        {
            internal SingleRunFastConfig(ITestOutputHelper output)
            {
                Add(Job.Dry);
                Add(Loggers.ConsoleLogger.Default);
                Add(new OutputLogger(output));
            }
        }
    }
}