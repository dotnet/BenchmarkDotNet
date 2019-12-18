using BenchmarkDotNet.Engines;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Reports
{
    public static class MeasurementExtensions
    {
        private const int NanosecondsInSecond = 1000 * 1000 * 1000;

        /// <summary>
        /// Gets the number of operations performed per second (ops/sec).
        /// </summary>
        public static double GetOpsPerSecond(this Measurement report) =>
            report.Operations / (report.Nanoseconds / NanosecondsInSecond);

        public static bool Is(this Measurement measurement, IterationMode mode, IterationStage stage)
            => measurement.IterationMode == mode && measurement.IterationStage == stage;

        [PublicAPI] public static bool IsOverhead(this Measurement measurement) => measurement.IterationMode == IterationMode.Overhead;
        [PublicAPI] public static bool IsWorkload(this Measurement measurement) => measurement.IterationMode == IterationMode.Workload;
    }
}