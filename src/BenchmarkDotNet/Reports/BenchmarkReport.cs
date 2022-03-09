using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        public BenchmarkCase BenchmarkCase { get; }
        public IReadOnlyList<Measurement> AllMeasurements { get; }
        public GcStats GcStats { get; }
        [PublicAPI] public bool Success { get; }
        [PublicAPI] public GenerateResult GenerateResult { get; }
        [PublicAPI] public BuildResult BuildResult { get; }
        [PublicAPI] public IReadOnlyDictionary<string, Metric> Metrics { get; }

        [NotNull]
        public IReadOnlyList<ExecuteResult> ExecuteResults { get; }

        [CanBeNull]
        public Statistics ResultStatistics => resultStatistics ?? (resultStatistics = GetResultRuns().Any()
            ? new Statistics(GetResultRuns().Select(r => r.GetAverageTime().Nanoseconds))
            : null);

        private Statistics resultStatistics;

        public BenchmarkReport(
            bool success,
            BenchmarkCase benchmarkCase,
            GenerateResult generateResult,
            BuildResult buildResult,
            IReadOnlyList<ExecuteResult> executeResults,
            IReadOnlyList<Metric> metrics)
        {
            Success = success;
            BenchmarkCase = benchmarkCase;
            GenerateResult = generateResult;
            BuildResult = buildResult;
            ExecuteResults = executeResults ?? Array.Empty<ExecuteResult>();
            AllMeasurements = ExecuteResults.SelectMany((results, index) => results.Measurements).ToArray();
            GcStats = ExecuteResults.Count > 0 ? executeResults[executeResults.Count -1].GcStats : default;
            Metrics = metrics?.ToDictionary(metric => metric.Descriptor.Id)
                ?? (IReadOnlyDictionary<string, Metric>)ImmutableDictionary<string, Metric>.Empty;
        }

        public override string ToString() => $"{BenchmarkCase.DisplayInfo}, {AllMeasurements.Count} runs";

        public IReadOnlyList<Measurement> GetResultRuns() => AllMeasurements.Where(r => r.Is(IterationMode.Workload, IterationStage.Result)).ToList();
    }
}