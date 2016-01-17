using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Common;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Reports
{
    public sealed class BenchmarkReport
    {
        public Benchmark Benchmark { get; }
        public IList<BenchmarkRunReport> Runs { get; }
        public BenchmarkParameters Parameters { get; }
        // TODO: parse real benchmark environment info
        public EnvironmentInfo HostInfo { get; }

        public BenchmarkReport(Benchmark benchmark, IList<BenchmarkRunReport> runs, EnvironmentInfo hostInfo, BenchmarkParameters parameters = null)
        {
            Benchmark = benchmark;
            Runs = runs;
            Parameters = parameters;
            HostInfo = hostInfo;
        }

        public static BenchmarkReport CreateEmpty(Benchmark benchmark, BenchmarkParameters parameters) =>
            new BenchmarkReport(benchmark, new BenchmarkRunReport[0], EnvironmentInfo.GetCurrentInfo(), parameters);
    }

    public static class BenchmarkReportExtensions
    {
        public static IList<BenchmarkRunReport> GetTargetRuns(this BenchmarkReport report) =>
            report.Runs.Where(r => r.IterationMode == BenchmarkIterationMode.Target).ToList();
    }
}