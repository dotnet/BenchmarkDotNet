using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using Perfolizer.Mathematics.OutlierDetection;

namespace BenchmarkDotNet.Engines
{
    public readonly struct RunResults(IReadOnlyList<Measurement> engineMeasurements, OutlierMode outlierMode, GcStats gcStats)
    {
        private readonly OutlierMode outlierMode = outlierMode;

        [PublicAPI]
        public IReadOnlyList<Measurement>? EngineMeasurements { get; } = engineMeasurements;

        [PublicAPI]
        public IReadOnlyList<Measurement>? Overhead
            => EngineMeasurements
                ?.Where(m => m.Is(IterationMode.Overhead, IterationStage.Actual))
                .ToArray();

        [PublicAPI]
        public IReadOnlyList<Measurement>? Workload
            => EngineMeasurements
                ?.Where(m => m.Is(IterationMode.Workload, IterationStage.Actual))
                .ToArray();

        public GcStats GCStats { get; } = gcStats;

        public IEnumerable<Measurement> GetWorkloadResultMeasurements()
        {
            var overheadActualMeasurements = Overhead ?? [];
            var workloadActualMeasurements = Workload ?? [];
            if (workloadActualMeasurements.IsEmpty())
                yield break;

            double overhead = overheadActualMeasurements.IsEmpty() ? 0.0 : new Statistics(overheadActualMeasurements.Select(m => m.Nanoseconds)).Median;
            var mainStats = new Statistics(workloadActualMeasurements.Select(m => m.Nanoseconds));
            int resultIndex = 0;
            foreach (var measurement in workloadActualMeasurements)
            {
                if (mainStats.IsActualOutlier(measurement.Nanoseconds, outlierMode))
                    continue;
                double value = Math.Max(0, measurement.Nanoseconds - overhead);
                if (IsSuspiciouslySmall(value))
                    value = 0;

                yield return new Measurement(
                    measurement.LaunchIndex,
                    IterationMode.Workload,
                    IterationStage.Result,
                    ++resultIndex,
                    measurement.Operations,
                    value);
            }
        }

        public IEnumerable<Measurement> GetAllMeasurements()
        {
            foreach (var measurement in EngineMeasurements ?? [])
                yield return measurement;
            foreach (var measurement in GetWorkloadResultMeasurements())
                yield return measurement;
        }

        internal async ValueTask WriteAsync(IHost host)
        {
            foreach (var measurement in GetWorkloadResultMeasurements())
            {
                await host.WriteLineAsync(measurement.ToString());
            }

            if (!GCStats.Equals(GcStats.Empty))
            {
                await host.WriteLineAsync(GCStats.ToOutputLine());
            }

            await host.WriteLineAsync();
        }

        // TODO: improve
        // If we get value < 0.1ns, it's probably a random noise, the actual value is 0.0ns.
        private static bool IsSuspiciouslySmall(double value) => value < 0.1;
    }
}