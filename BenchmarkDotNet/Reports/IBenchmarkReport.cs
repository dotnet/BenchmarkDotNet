using System.Collections.Generic;

namespace BenchmarkDotNet.Reports
{
    public interface IBenchmarkReport
    {
        IBenchmark Benchmark { get; }
        IList<IBenchmarkRunReport> WarmUp { get; }
        IList<IBenchmarkRunReport> Target { get; }
    }
}