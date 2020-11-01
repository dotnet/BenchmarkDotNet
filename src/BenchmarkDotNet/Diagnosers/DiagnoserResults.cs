using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Diagnosers
{
    public class DiagnoserResults
    {
        public DiagnoserResults(BenchmarkCase benchmarkCase, long totalOperations, GcStats gcStats,
            ThreadingStats threadingStats, BuildResult buildResult)
        {
            BenchmarkCase = benchmarkCase;
            TotalOperations = totalOperations;
            GcStats = gcStats;
            ThreadingStats = threadingStats;
            BuildResult = buildResult;
        }

        public BenchmarkCase BenchmarkCase { get; }

        public long TotalOperations { get; }

        public GcStats GcStats { get; }

        public ThreadingStats ThreadingStats { get; }

        public BuildResult BuildResult { get; }
    }
}