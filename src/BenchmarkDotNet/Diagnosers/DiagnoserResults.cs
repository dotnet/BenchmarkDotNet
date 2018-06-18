using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnosers
{
    public class DiagnoserResults
    {
        public DiagnoserResults(BenchmarkCase benchmarkCase, long totalOperations, GcStats gcStats)
        {
            BenchmarkCase = benchmarkCase;
            TotalOperations = totalOperations;
            GcStats = gcStats;
        }

        public BenchmarkCase BenchmarkCase { get; }

        public long TotalOperations { get; }

        public GcStats GcStats { get; }
    }
}