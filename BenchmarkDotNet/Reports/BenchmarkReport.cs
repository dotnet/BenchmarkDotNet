using System.Collections.Generic;

namespace BenchmarkDotNet.Reports
{
    internal sealed class BenchmarkReport : IBenchmarkReport
    {
        public IBenchmark Benchmark { get; }
        public IList<IBenchmarkRunReport> WarmUp { get; }
        public IList<IBenchmarkRunReport> Target { get; }

        public BenchmarkReport(IBenchmark benchmark, IList<IBenchmarkRunReport> warmUp, IList<IBenchmarkRunReport> target)
        {
            Benchmark = benchmark;
            WarmUp = warmUp;
            Target = target;
        }
    }
}