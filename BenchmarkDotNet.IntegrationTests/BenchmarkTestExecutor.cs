using System.Linq;
using BenchmarkDotNet.Running;
using Xunit;

namespace BenchmarkDotNet.IntegrationTests
{
    internal static class BenchmarkTestExecutor
    {
        /// <summary>
        /// runs Benchmarks with the most simple config
        /// and verifies if every benchmark got successfully executed
        /// </summary>
        /// <typeparam name="TBenchmark">type that defines Benchmarks</typeparam>
        internal static void CanExecute<TBenchmark>()
        {
            var summary = BenchmarkRunner.Run<TBenchmark>(new SingleRunFastConfig());

            Assert.False(summary.HasCriticalValidationErrors);
            Assert.True(summary.Reports.Any());
            Assert.True(summary
                .Reports
                .All(report =>
                    report
                        .ExecuteResults
                        .Any(executeResult => executeResult.FoundExecutable && executeResult.Data.Any())));
            Assert.True(summary.Reports.All(report => report.AllMeasurements.Any()));
        }
    }
}