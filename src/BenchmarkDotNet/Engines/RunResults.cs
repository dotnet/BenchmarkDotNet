using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    public struct RunResults
    {
        private readonly OutlierMode outlierMode;
        private readonly Encoding encoding;
        [CanBeNull]
        public IReadOnlyList<Measurement> Idle { get; }

        [NotNull]
        public IReadOnlyList<Measurement> Main { get; }

        public GcStats GCStats { get; }

        public RunResults([CanBeNull] IReadOnlyList<Measurement> idle,
                          [NotNull] IReadOnlyList<Measurement> main,
                          OutlierMode outlierMode,
                          GcStats gcStats,
                          Encoding encoding)
        {
            this.outlierMode = outlierMode;
            this.encoding = encoding;
            Idle = idle;
            Main = main;
            GCStats = gcStats;
        }

        public IEnumerable<Measurement> GetMeasurements()
        {
            double overhead = Idle == null ? 0.0 : new Statistics(Idle.Select(m => m.Nanoseconds)).Mean;
            var mainStats = new Statistics(Main.Select(m => m.Nanoseconds));
            int resultIndex = 0;
            foreach (var measurement in Main)
            {
                if (mainStats.IsActualOutlier(measurement.Nanoseconds, outlierMode))
                    continue;

                double value = Math.Max(0, measurement.Nanoseconds - overhead);
                if (IsSuspiciouslySmall(value))
                    value = 0;

                yield return new Measurement(
                    measurement.LaunchIndex,
                    IterationMode.Result,
                    ++resultIndex,
                    measurement.Operations,
                    value,
                    encoding);
            }
        }

        public void Print(TextWriter outWriter)
        {
            foreach (var measurement in GetMeasurements())
                outWriter.WriteLine(measurement.ToOutputLine());

            outWriter.WriteLine(GCStats.ToOutputLine());
            outWriter.WriteLine();
        }

        // TODO: improve
        // If we get value < 0.1ns, it's probably a random noise, the actual value is 0.0ns.
        private static bool IsSuspiciouslySmall(double value) => value < 0.1;
    }
}