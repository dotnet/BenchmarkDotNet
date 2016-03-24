using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.IntegrationTests.CustomPaths;
using BenchmarkDotNet.IntegrationTests.DifferentRuntime;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests.Classic
{
    public class References
    {
        [Fact]
        public void BenchmarksThatUseTypeFromCustomPathDllAreSupported()
        {
            CanCompileAndRun<BenchmarksThatUseTypeFromCustomPathDll>();
        }

        [Fact]
        public void BenchmarksThatReturnTypeFromCustomPathDllAreSupported()
        {
            CanCompileAndRun<BenchmarksThatReturnTypeFromCustomPathDll>();
        }

        [Fact]
        public void BenchmarksThatReturnTypeThatRequiresDifferentRuntimeAreSupported()
        {
            CanCompileAndRun<BenchmarksThatReturnTypeThatRequiresDifferentRuntime>();
        }

        [Fact]
        public void BenchmarksThatUseTypeThatRequiresDifferentRuntimeAreSupported()
        {
            CanCompileAndRun<BenchmarksThatUseTypeThatRequiresDifferentRuntime>();
        }

        private void CanCompileAndRun<TBenchmark>()
        {
            var summary = BenchmarkRunner.Run<TBenchmark>(new SingleRunFastConfig());

            Assert.True(summary.Reports.Any());
            Assert.True(summary.Reports.All(report => report.Value.ExecuteResults.All(executeResult => executeResult.FoundExecutable)));
            Assert.True(summary.Reports.All(report => report.Value.AllMeasurements.Any()));
        }

        private class SingleRunFastConfig : ManualConfig
        {
            internal SingleRunFastConfig()
            {
                Add(Job.Dry);
            }
        }
    }
}
