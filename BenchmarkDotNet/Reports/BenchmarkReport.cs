using System.Collections.Generic;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Reports
{
    public sealed class BenchmarkReport
    {
        public Benchmark Benchmark { get; }
        public IList<BenchmarkRunReport> Runs { get; }
        public BenchmarkParameters Parameters { get; }

        public BenchmarkReport(Benchmark benchmark, IList<BenchmarkRunReport> runs, BenchmarkParameters parameters = null)
        {
            Benchmark = benchmark;
            Runs = runs;
            Parameters = parameters;
        }

        public static BenchmarkReport CreateEmpty(Benchmark benchmark, BenchmarkParameters parameters) => new BenchmarkReport(benchmark, new BenchmarkRunReport[0], parameters);
    }
}