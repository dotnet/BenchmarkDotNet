using System;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Reports
{
    // TODO: add processIndex
    /// <summary>
    /// The basic captured statistics for a benchmark.
    /// </summary>
    public sealed class BenchmarkRunReport
    {
        public IterationMode IterationMode { get; }

        public int ProcessIndex { get; }

        public int IterationIndex { get; }

        /// <summary>
        /// Gets the number of operations performed.
        /// </summary>
        public long Operations { get; }

        /// <summary>
        /// Gets the total number of nanoseconds it took to perform all operations.
        /// </summary>
        public double Nanoseconds { get; }

        /// <summary>
        /// Creates an instance of <see cref="BenchmarkRunReport"/> class.
        /// </summary>
        /// <param name="processIndex"></param>
        /// <param name="iterationMode"></param>
        /// <param name="iterationIndex"></param>
        /// <param name="operations">The number of operations performed.</param>
        /// <param name="nanoseconds">The total number of nanoseconds it took to perform all operations.</param>
        public BenchmarkRunReport(int processIndex, IterationMode iterationMode, int iterationIndex, long operations, double nanoseconds)
        {
            IterationMode = iterationMode;
            IterationIndex = iterationIndex;
            Operations = operations;
            Nanoseconds = nanoseconds;
            ProcessIndex = processIndex;
        }

        /// <summary>
        /// Parses the benchmark statistics from the plain text line.
        /// 
        /// E.g. given the input <paramref name="line"/>:
        /// 
        ///     Target 1: 10 op, 1005842518 ns
        /// 
        /// Will extract the number of <see cref="Operations"/> performed and the 
        /// total number of <see cref="Nanoseconds"/> it took to perform them.
        /// </summary>
        /// <param name="logger">The logger to write any diagnostic messages to.</param>
        /// <param name="line">The line to parse.</param>
        /// <returns>An instance of <see cref="BenchmarkRunReport"/> if parsed successfully. <c>Null</c> in case of any trouble.</returns>
        public static BenchmarkRunReport Parse(ILogger logger, string line, int processIndex)
        {
            try
            {
                var lineSplit = line.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                var iterationInfo = lineSplit[0];
                var iterationInfoSplit = iterationInfo.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var iterationMode = ParseIterationMode(iterationInfoSplit[0]);
                var iterationIndex = 0;
                int.TryParse(iterationInfoSplit[1], out iterationIndex);

                var measurementsInfo = lineSplit[1];
                var measurementsInfoSplit = measurementsInfo.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var op = 1L;
                var ns = double.PositiveInfinity;
                foreach (var item in measurementsInfoSplit)
                {
                    var measurementSplit = item.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var value = measurementSplit[0];
                    var unit = measurementSplit[1];
                    switch (unit)
                    {
                        case "ns":
                            ns = double.Parse(value, EnvironmentHelper.MainCultureInfo);
                            break;
                        case "op":
                            op = long.Parse(value);
                            break;
                    }
                }
                return new BenchmarkRunReport(processIndex, iterationMode, iterationIndex, op, ns);
            }
            catch (Exception)
            {
                logger.WriteLineError("Parse error in the following line:");
                logger.WriteLineError(line);
                return null;
            }
        }

        private static IterationMode ParseIterationMode(string name)
        {
            IterationMode mode;
            return Enum.TryParse(name, out mode) ? mode : IterationMode.Unknown;
        }
    }

    public static class BenchmarkRunReportExtensions
    {
        private const int NanosecondsInSecond = 1000 * 1000 * 1000;

        /// <summary>
        /// Gets the number of operations performed per second (ops/sec).
        /// </summary>
        public static double GetOpsPerSecond(this BenchmarkRunReport report) =>
            report.Operations / (report.Nanoseconds / NanosecondsInSecond);

        /// <summary>
        /// Gets the average duration of one operation in nanoseconds.
        /// </summary>
        public static double GetAverageNanoseconds(this BenchmarkRunReport report) =>
            report.Nanoseconds / report.Operations;

        public static string ToStr(this BenchmarkRunReport run) =>
            $"{run.IterationMode} {run.IterationIndex}: {run.Operations} op, {run.Nanoseconds.ToStr()} ns, {run.GetAverageNanoseconds().ToTimeStr()}/op";

    }
}