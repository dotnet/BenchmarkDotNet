using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Diagnosers
{
    public class DiagnoserResults
    {
        public DiagnoserResults(BenchmarkCase benchmarkCase, long totalOperations, GcStats gcStats, ThreadingStats threadingStats)
        {
            BenchmarkCase = benchmarkCase;
            TotalOperations = totalOperations;
            GcStats = gcStats;
            ThreadingStats = threadingStats;
        }

        public BenchmarkCase BenchmarkCase { get; }

        public long TotalOperations { get; }

        public GcStats GcStats { get; }

        public ThreadingStats ThreadingStats { get; }
    }
}