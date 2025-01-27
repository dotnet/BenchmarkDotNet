using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Phd;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;
using JetBrains.Annotations;
using Perfolizer.Horology;
using Perfolizer.Phd;
using Perfolizer.Phd.Base;
using Perfolizer.Phd.Dto;

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

        public IReadOnlyList<ExecuteResult> ExecuteResults { get; }

        public Statistics? ResultStatistics => resultStatistics ??=
            GetResultRuns().Any()
                ? new Statistics(GetResultRuns().Select(r => r.GetAverageTime().Nanoseconds))
                : null;

        private Statistics? resultStatistics = null;

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
            GcStats = ExecuteResults.Count > 0 ? ExecuteResults[ExecuteResults.Count - 1].GcStats : default;
            Metrics = metrics?.ToDictionary(metric => metric.Descriptor.Id)
                      ?? (IReadOnlyDictionary<string, Metric>)ImmutableDictionary<string, Metric>.Empty;
        }

        public override string ToString() => $"{BenchmarkCase.DisplayInfo}, {AllMeasurements.Count} runs";

        public IReadOnlyList<Measurement> GetResultRuns() => AllMeasurements.Where(r => r.Is(IterationMode.Workload, IterationStage.Result)).ToList();

        public PhdEntry ToPhd()
        {
            var entry = new PhdEntry
            {
                Benchmark = new BdnBenchmark
                {
                    Display = BenchmarkCase.DisplayInfo,
                    Namespace = BenchmarkCase.Descriptor.Type.Namespace ?? "",
                    Type = FullNameProvider.GetTypeName(BenchmarkCase.Descriptor.Type),
                    Method = BenchmarkCase.Descriptor.WorkloadMethod.Name,
                    Parameters = BenchmarkCase.Parameters.PrintInfo,
                    HardwareIntrinsics = this.GetHardwareIntrinsicsInfo()
                },
                Job = new BdnJob
                {
                    Environment = BenchmarkCase.Job.Environment.ToPhd(),
                    Execution = BenchmarkCase.Job.Run.ToPhd()
                }
            };

            var lifecycles = AllMeasurements.GroupBy(m => new BdnLifecycle
            {
                LaunchIndex = m.LaunchIndex,
                IterationMode = m.IterationMode,
                IterationStage = m.IterationStage
            }).OrderBy(x => x.Key).ToList();
            foreach (var lifecycleGroup in lifecycles)
            {
                var measurementsEntry = new PhdEntry
                {
                    Lifecycle = lifecycleGroup.Key
                };

                foreach (var measurement in lifecycleGroup.ToList())
                {
                    measurementsEntry.Add(new PhdEntry
                    {
                        IterationIndex = measurement.IterationIndex,
                        InvocationCount = measurement.Operations,
                        Value = measurement.Nanoseconds / measurement.Operations,
                        Unit = TimeUnit.Nanosecond
                    });
                }
                entry.Add(measurementsEntry);
            }

            return entry;
        }
    }
}