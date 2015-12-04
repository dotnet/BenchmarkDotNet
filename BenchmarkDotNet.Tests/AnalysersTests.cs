using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Plugins.Analyzers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests
{
    public class AnalysersTests
    {
        private readonly ITestOutputHelper output;

        public AnalysersTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void StdDevTest()
        {
            // TODO: write a mock for benchmark reports
            var report = new BenchmarkReport(
                new Benchmark(
                    new BenchmarkTarget(null, null),
                    new BenchmarkTask(1,
                    new BenchmarkConfiguration(
                        BenchmarkMode.SingleRun,
                        BenchmarkPlatform.AnyCpu,
                        BenchmarkJitVersion.HostJit,
                        BenchmarkFramework.HostFramework,
                        BenchmarkToolchain.Classic,
                        BenchmarkRuntime.Clr,
                        1,
                        1))),
                new List<BenchmarkRunReport>
                {
                    new BenchmarkRunReport(1, 10),
                    new BenchmarkRunReport(1, 50),
                    new BenchmarkRunReport(1, 100)
                });
            var reports = new[] { report };
            var warnings = new BenchmarkStdDevAnalyser().Analyze(reports).ToList();
            Assert.Equal(1, warnings.Count);
            foreach (var warning in warnings)
                output.WriteLine($"[{warning.Kind}] {warning.Message}");
        }
    }
}