using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using Perfolizer.Mathematics.OutlierDetection;

namespace BenchmarkDotNet.Engines
{
    public struct RunResults
    {
        private readonly OutlierMode outlierMode;

        [PublicAPI]
        public IReadOnlyList<Measurement> EngineMeasurements { get; }

        [PublicAPI]
        public IReadOnlyList<Measurement>? Overhead
            => EngineMeasurements
                .Where(m => m.Is(IterationMode.Overhead, IterationStage.Actual))
                .ToArray();

        [PublicAPI]
        public IReadOnlyList<Measurement> Workload
            => EngineMeasurements
                .Where(m => m.Is(IterationMode.Workload, IterationStage.Actual))
                .ToArray();

        public GcStats GCStats { get; }

        public ThreadingStats ThreadingStats { get; }

        public double ExceptionFrequency { get; }

        public RunResults(IReadOnlyList<Measurement> engineMeasurements,
            OutlierMode outlierMode,
            GcStats gcStats,
            ThreadingStats threadingStats,
            double exceptionFrequency)
        {
            this.outlierMode = outlierMode;
            EngineMeasurements = engineMeasurements;
            GCStats = gcStats;
            ThreadingStats = threadingStats;
            ExceptionFrequency = exceptionFrequency;
        }

        public IEnumerable<Measurement> GetWorkloadResultMeasurements()
        {
            var overheadActualMeasurements = Overhead ?? Array.Empty<Measurement>();
            var workloadActualMeasurements = Workload;
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
            foreach (var measurement in EngineMeasurements)
                yield return measurement;
            foreach (var measurement in GetWorkloadResultMeasurements())
                yield return measurement;
        }

        public void Print(TextWriter outWriter)
        {
            foreach (var measurement in GetWorkloadResultMeasurements())
                outWriter.WriteLine(measurement.ToString());

            if (!GCStats.Equals(GcStats.Empty))
                outWriter.WriteLine(GCStats.ToOutputLine());
            if (!ThreadingStats.Equals(ThreadingStats.Empty))
                outWriter.WriteLine(ThreadingStats.ToOutputLine());
            if (ExceptionFrequency > 0)
                outWriter.WriteLine(ExceptionsStats.ToOutputLine(ExceptionFrequency));

            outWriter.WriteLine();
        }

        // TODO: improve
        // If we get value < 0.1ns, it's probably a random noise, the actual value is 0.0ns.
        private static bool IsSuspiciouslySmall(double value) => value < 0.1;
    }
}