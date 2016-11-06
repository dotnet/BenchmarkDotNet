using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    public struct RunResults
    {
        private readonly bool removeOutliers;
        private readonly long totalOperationsCount;

        [CanBeNull]
        public List<Measurement> Idle { get; }

        [NotNull]
        public List<Measurement> Main { get; }

        public GcStats GCStats { get; }

        public RunResults(
            [CanBeNull] List<Measurement> idle, [NotNull] List<Measurement> main, bool removeOutliers, GcStats gcStats)
        {
            this.removeOutliers = removeOutliers;
            Idle = idle;
            Main = main;
            GCStats = gcStats;

            totalOperationsCount = 0;
            foreach (var measurement in Main)
            {
                if (!measurement.IterationMode.IsIdle())
                    totalOperationsCount += measurement.Operations;
            }
        }

        // TODO: rewrite without allocations
        public IEnumerable<Measurement> GetMeasurements()
        {
            double overhead = Idle == null ? 0.0 : new Statistics(Idle.Select(m => m.Nanoseconds)).Mean;
            var mainStats = new Statistics(Main.Select(m => m.Nanoseconds));
            int resultIndex = 0;
            foreach (var measurement in Main)
            {
                if (removeOutliers && mainStats.IsOutlier(measurement.Nanoseconds))
                    continue;

                double value = Math.Max(0, measurement.Nanoseconds - overhead);
                if (IsSuspiciouslySmall(value))
                    value = 0;

                yield return new Measurement(
                    measurement.LaunchIndex,
                    IterationMode.Result,
                    ++resultIndex,
                    measurement.Operations,
                    value);
            }
        }

        public void Print()
        {
            foreach (var measurement in GetMeasurements())
                WriteLine(measurement.ToOutputLine());

            WriteLine(GCStats.WithTotalOperations(totalOperationsCount).ToOutputLine());
            WriteLine();
        }

        private void WriteLine() => Console.WriteLine();
        private void WriteLine(string line) => Console.WriteLine(line);

        // TODO: improve
        // If we get value < 0.1ns, it's probably a random noise, the actual value is 0.0ns.
        private static bool IsSuspiciouslySmall(double value) => value < 0.1;
    }
}