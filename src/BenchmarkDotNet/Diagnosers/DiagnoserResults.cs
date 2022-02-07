using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using System.Linq;

namespace BenchmarkDotNet.Diagnosers
{
    public class DiagnoserResults
    {
        public DiagnoserResults(BenchmarkCase benchmarkCase, ExecuteResult executeResult, BuildResult buildResult)
        {
            BenchmarkCase = benchmarkCase;
            TotalOperations = executeResult.Measurements.Where(measurement => measurement.IsWorkload()).Sum(m => m.Operations);
            GcStats = executeResult.GcStats;
            ThreadingStats = executeResult.ThreadingStats;
            BuildResult = buildResult;
        }

        public BenchmarkCase BenchmarkCase { get; }

        public long TotalOperations { get; }

        public GcStats GcStats { get; }

        public ThreadingStats ThreadingStats { get; }

        public BuildResult BuildResult { get; }
    }
}