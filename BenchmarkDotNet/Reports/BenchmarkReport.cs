using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace BenchmarkDotNet.Reports
{
    public sealed class BenchmarkReport
    {
        public Benchmark Benchmark { get; }
        public IList<BenchmarkRunReport> AllRuns { get; }

        public GenerateResult GenerateResult { get; }
        public BuildResult BuildResult { get; }
        public IList<ExecuteResult> ExecuteResults { get; }

        public Statistics TargetStatistics => this.GetTargetRuns().Any() ? new Statistics(this.GetTargetRuns().Select(r => r.Nanoseconds)) : null;

        public BenchmarkReport(
            Benchmark benchmark,
            GenerateResult generateResult,
            BuildResult buildResult,
            IList<ExecuteResult> executeResults,
            IList<BenchmarkRunReport> allRuns)
        {
            Benchmark = benchmark;
            GenerateResult = generateResult;
            BuildResult = buildResult;
            ExecuteResults = executeResults;
            AllRuns = allRuns ?? new BenchmarkRunReport[0];
        }
    }

    public static class BenchmarkReportExtensions
    {
        public static IList<BenchmarkRunReport> GetTargetRuns(this BenchmarkReport report) =>
            report.AllRuns.Where(r => r.IterationMode == IterationMode.Target).ToList();
    }
}