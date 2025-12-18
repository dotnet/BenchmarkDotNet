using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using System.Collections.Generic;

namespace BenchmarkDotNet.Diagnosers
{
    public class DiagnoserResults(BenchmarkCase benchmarkCase, ExecuteResult executeResult, BuildResult buildResult)
    {
        public BenchmarkCase BenchmarkCase { get; } = benchmarkCase;

        public GcStats GcStats { get; } = executeResult.GcStats;

        public BuildResult BuildResult { get; } = buildResult;

        public IReadOnlyList<Measurement> Measurements { get; } = executeResult.Measurements;
    }
}