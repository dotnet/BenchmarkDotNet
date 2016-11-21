using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Reports
{
    public sealed class BenchmarkReport
    {
        public Benchmark Benchmark { get; }
        public IReadOnlyList<Measurement> AllMeasurements { get; }

        public GenerateResult GenerateResult { get; }
        public BuildResult BuildResult { get; }

        [NotNull]
        public IReadOnlyList<ExecuteResult> ExecuteResults { get; }

        public Statistics ResultStatistics => resultStatistics ?? (resultStatistics = GetResultRuns().Any()
            ? new Statistics(GetResultRuns().Select(r => r.GetAverageNanoseconds()))
            : null);

        private Statistics resultStatistics;

        public BenchmarkReport(
            Benchmark benchmark,
            GenerateResult generateResult,
            BuildResult buildResult,
            IReadOnlyList<ExecuteResult> executeResults,
            IReadOnlyList<Measurement> allMeasurements)
        {
            Benchmark = benchmark;
            GenerateResult = generateResult;
            BuildResult = buildResult;
            ExecuteResults = executeResults ?? new ExecuteResult[0];
            AllMeasurements = allMeasurements ?? new Measurement[0];
        }

        public override string ToString() => $"{Benchmark.DisplayInfo}, {AllMeasurements.Count} runs";

        public IReadOnlyList<Measurement> GetResultRuns() => AllMeasurements.Where(r => r.IterationMode == IterationMode.Result).ToList();
    }
}