using System.Collections.Generic;

namespace BenchmarkDotNet.Reports
{
    public sealed class BenchmarkReport
    {
        public Benchmark Benchmark { get; }
        public IList<BenchmarkRunReport> Runs { get; }

        public BenchmarkReport(Benchmark benchmark, IList<BenchmarkRunReport> runs)
        {
            Benchmark = benchmark;
            Runs = runs;
        }
    }
}