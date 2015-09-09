using System.Collections.Generic;

namespace BenchmarkDotNet.Reports
{
    public sealed class BenchmarkReport
    {
        public Benchmark Benchmark { get; }
        public IList<BenchmarkRunReport> Runs { get; }
        public int? BenchmarkParam { get; }

        public BenchmarkReport(Benchmark benchmark, IList<BenchmarkRunReport> runs, int? benchmarkParam = null)
        {
            Benchmark = benchmark;
            Runs = runs;
            BenchmarkParam = benchmarkParam;
        }
    }
}