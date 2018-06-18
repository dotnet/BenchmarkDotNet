using System;
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
        public BenchmarkCase BenchmarkCase { get; }
        public IReadOnlyList<Measurement> AllMeasurements { get; }
        public GcStats GcStats { get; }

        public GenerateResult GenerateResult { get; }
        public BuildResult BuildResult { get; }

        [NotNull]
        public IReadOnlyList<ExecuteResult> ExecuteResults { get; }

        [CanBeNull]
        public Statistics ResultStatistics => resultStatistics ?? (resultStatistics = GetResultRuns().Any()
            ? new Statistics(GetResultRuns().Select(r => r.GetAverageNanoseconds()))
            : null);

        private Statistics resultStatistics;

        public BenchmarkReport(
            BenchmarkCase benchmarkCase,
            GenerateResult generateResult,
            BuildResult buildResult,
            IReadOnlyList<ExecuteResult> executeResults,
            IReadOnlyList<Measurement> allMeasurements, 
            GcStats gcStats)
        {
            BenchmarkCase = benchmarkCase;
            GenerateResult = generateResult;
            BuildResult = buildResult;
            ExecuteResults = executeResults ?? Array.Empty<ExecuteResult>();
            AllMeasurements = allMeasurements ?? Array.Empty<Measurement>();
            GcStats = gcStats;
        }

        public override string ToString() => $"{BenchmarkCase.DisplayInfo}, {AllMeasurements.Count} runs";

        public IReadOnlyList<Measurement> GetResultRuns() => AllMeasurements.Where(r => r.IterationMode == IterationMode.Result).ToList();
    }
}