using System.Text;
using BenchmarkDotNet.Extensions;

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
            $"{run.IterationMode} {run.IterationIndex}: {run.Operations} op, {run.Nanoseconds.ToStr()} ns, {run.GetAverageNanoseconds().ToTimeStr(encoding ?? Encoding.ASCII)}/op";

    }
}