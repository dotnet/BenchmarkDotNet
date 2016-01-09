using System.Collections.Generic;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Reports
{
    public sealed class BenchmarkReport
    {
        public Benchmark Benchmark { get; }
        public IList<BenchmarkRunReport> Runs { get; }
        public BenchmarkParameters Parameters { get; }
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
}