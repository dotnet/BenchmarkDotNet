using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;
using Perfolizer.Mathematics.OutlierDetection;

namespace BenchmarkDotNet.Engines
{
    public struct RunResults
    {
        private readonly OutlierMode outlierMode;

        [CanBeNull, PublicAPI]
        public IReadOnlyList<Measurement> Overhead { get; }

        [NotNull, PublicAPI]
        public IReadOnlyList<Measurement> Workload { get; }

        public GcStats GCStats { get; }

        public ThreadingStats ThreadingStats { get; }

        public RunResults([CanBeNull] IReadOnlyList<Measurement> overhead,
                          [NotNull] IReadOnlyList<Measurement> workload,
                          OutlierMode outlierMode,
                          GcStats gcStats,
                          ThreadingStats threadingStats)
        {
            this.outlierMode = outlierMode;
            Overhead = overhead;
            Workload = workload;
            GCStats = gcStats;
            ThreadingStats = threadingStats;
        }

        public IEnumerable<Measurement> GetMeasurements()
        {
            double overhead = Overhead == null ? 0.0 : new Statistics(Overhead.Select(m => m.Nanoseconds)).Median;
            var mainStats = new Statistics(Workload.Select(m => m.Nanoseconds));
            int resultIndex = 0;
            foreach (var measurement in Workload)
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

        public void Print(TextWriter outWriter)
        {
            foreach (var measurement in GetMeasurements())
                outWriter.WriteLine(measurement.ToString());

            if (!GCStats.Equals(GcStats.Empty))
                outWriter.WriteLine(GCStats.ToOutputLine());
            if (!ThreadingStats.Equals(ThreadingStats.Empty))
                outWriter.WriteLine(ThreadingStats.ToOutputLine());

            outWriter.WriteLine();
        }

        // TODO: improve
        // If we get value < 0.1ns, it's probably a random noise, the actual value is 0.0ns.
        private static bool IsSuspiciouslySmall(double value) => value < 0.1;
    }
}