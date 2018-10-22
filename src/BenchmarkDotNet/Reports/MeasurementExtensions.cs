using System.Text;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
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

        /// <summary>
        /// Gets the average duration of one operation in nanoseconds.
        /// </summary>
        public static double GetAverageNanoseconds(this Measurement report) =>
            report.Nanoseconds / report.Operations;

        //TODO OPTIONAL ENCODING
        public static string ToStr(this Measurement run, Encoding encoding) =>
            $"{run.IterationMode}{run.IterationStage} {run.IterationIndex}: {run.Operations} op, {run.Nanoseconds.ToStr()} ns, {run.GetAverageNanoseconds().ToTimeStr(encoding ?? Encoding.ASCII)}/op";

        public static bool Is(this Measurement measurement, IterationMode mode, IterationStage stage)
            => measurement.IterationMode == mode && measurement.IterationStage == stage;

        [PublicAPI] public static bool IsOverhead(this Measurement measurement) => measurement.IterationMode == IterationMode.Overhead;
        [PublicAPI] public static bool IsWorkload(this Measurement measurement) => measurement.IterationMode == IterationMode.Workload;
    }
}