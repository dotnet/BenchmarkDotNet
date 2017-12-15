using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnosers
{
    public class DiagnoserResults
    {
        public DiagnoserResults(Benchmark benchmark, long totalOperations, GcStats gcStats)
        {
            Benchmark = benchmark;
            TotalOperations = totalOperations;
            GcStats = gcStats;
        }

        public Benchmark Benchmark { get; }

        public long TotalOperations { get; }

        public GcStats GcStats { get; }
    }
}