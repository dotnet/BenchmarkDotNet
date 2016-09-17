using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Reports
{
    public sealed class BenchmarkReport
    {
        public Benchmark Benchmark { get; }
        public IList<Measurement> AllMeasurements { get; }

        public GenerateResult GenerateResult { get; }
        public BuildResult BuildResult { get; }
        public IList<ExecuteResult> ExecuteResults { get; }

        public Statistics ResultStatistics => this.GetResultRuns().Any()
            ? new Statistics(this.GetResultRuns().Select(r => r.GetAverageNanoseconds()))
            : null;

        public BenchmarkReport(
            Benchmark benchmark,
            GenerateResult generateResult,
            BuildResult buildResult,
            IList<ExecuteResult> executeResults,
            IList<Measurement> allMeasurements)
        {
            Benchmark = benchmark;
            GenerateResult = generateResult;
            BuildResult = buildResult;
            ExecuteResults = executeResults;
            AllMeasurements = allMeasurements ?? new Measurement[0];
        }

        public override string ToString() => $"{Benchmark.DisplayInfo}, {AllMeasurements.Count} runs";
    }

    public static class BenchmarkReportExtensions
    {
        public static IList<Measurement> GetResultRuns(this BenchmarkReport report) =>
            report.AllMeasurements.Where(r => r.IterationMode == IterationMode.Result).ToList();
    }
}