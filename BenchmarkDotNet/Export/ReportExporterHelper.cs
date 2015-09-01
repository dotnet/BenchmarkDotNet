using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Export
{
    public static class ReportExporterHelper
    {
        public static List<string[]> BuildTable(IList<BenchmarkReport> reports, bool pretty = true)
        {
            var reportStats = reports.Where(r => r.Runs.Count > 0).Select(
             r => new
             {
                 r.Benchmark,
                 Stat = new BenchmarkRunReportsStatistic("Target", r.Runs)
             }).ToList();

            // Ensure uniform number formats and use of time units via these helpers.
            var averageTimeStats = reportStats.Select(reportStat => reportStat.Stat);
            var timeToStringFunc = GetTimeMeasurementFormattingFunc(averageTimeStats, pretty);
            var opsPerSecToStringFunc = GetOpsPerSecFormattingFunc();

            var table = new List<string[]> { new[] { "Type", "Method", "Mode", "Platform", "Jit", ".NET", "AvrTime", "StdDev", "op/s" } };
            foreach (var reportStat in reportStats)
            {
                var b = reportStat.Benchmark;

                string[] row = {
                    b.Target.Type.Name,
                    b.Target.Method.Name,
                    b.Task.Configuration.Mode.ToString(),
                    b.Task.Configuration.Platform.ToString(),
                    b.Task.Configuration.JitVersion.ToString(),
                    b.Task.Configuration.Framework.ToString(),
                    timeToStringFunc(reportStat.Stat.AverageTime.Median),
                    timeToStringFunc(reportStat.Stat.AverageTime.StandardDeviation),
                    opsPerSecToStringFunc(reportStat.Stat.OperationsPerSeconds.Median)
                };
                table.Add(row);
            }

            return table;
        }

        /// <summary>
        /// Given a list of benchmark statistics creates a function to convert 
        /// raw <see cref="AverageTime"/> measurements to string format so that they
        /// are shown using uniform time units and align nicely.
        /// The <see cref="AverageTime"/> measurements are assumed to contain time lengths in nanoseconds.
        /// </summary>
        /// <param name="statistics">The list of time-based <see cref="BenchmarkRunReportsStatistic"/>.</param>
        /// <returns>A function which should be used to convert all <see cref="AverageTime"/> measurements to string.</returns>
        /// <remarks>
        /// The measurements are formatted in such a way that they use the same time unit 
        /// the number of decimals so that they are easily comparable and align nicely.
        /// 
        /// Example:
        /// Consider we have the following raw input where numbers are durations in nanoseconds: 
        ///     Median=597855, StdErr=485;
        ///     Median=7643, StdErr=87;
        /// 
        /// When using the formatting function, the output will be like this:
        ///     597.8550 us, 0.0485 us;
        ///       7.6430 us, 0.0087 us;
        /// </remarks>
        public static Func<double, string> GetTimeMeasurementFormattingFunc(IEnumerable<BenchmarkRunReportsStatistic> statistics, bool pretty = true)
        {
            if (!pretty)
                return value => string.Format(EnvironmentHelper.MainCultureInfo, "{0:#.####}", value);

            // Find the smallest measurement in the primary statistics, which is the Median.
            // This will determine the time unit we will use for all measurements.
            var minRecordedMedian = statistics.Min(stat => stat.AverageTime.Median);

            // Use the largest unit to display the smallest recorded measurement without loss of precision.
            // TODO: This is duplicated in BenchmarkMeasurementStatistic.ToString(), figure out how to refactor this later?
            Func<double, string> measurementToString;
            if (minRecordedMedian < 1000)
            {
                measurementToString = (value) => string.Format(EnvironmentHelper.MainCultureInfo, "{0:N4} ns", value);
            }
            else if ((minRecordedMedian / 1000) < 1000)
            {
                measurementToString = (value) => string.Format(EnvironmentHelper.MainCultureInfo, "{0:N4} us", value / 1000);
            }
            else if ((minRecordedMedian / 1000 / 1000) < 1000)
            {
                measurementToString = (value) => string.Format(EnvironmentHelper.MainCultureInfo, "{0:N4} ms", value / 1000 / 1000);
            }
            else
            {
                measurementToString = (value) => string.Format(EnvironmentHelper.MainCultureInfo, "{0:N4}  s", value / 1000 / 1000 / 1000);
            }

            return measurementToString;
        }

        ///  <summary>
        /// Given a list of benchmark statistics creates a function to convert 
        /// raw <see cref="OperationsPerSeconds"/> measurements to string format so that they align nicely.
        ///  </summary>
        /// <returns>A function which should be used to convert all <see cref="OperationsPerSeconds"/> measurements to string.</returns>
        /// <remarks>
        ///  Ops/sec number formatting:
        ///       - Thousand separators: we generally expect large numbers so these 
        ///         would make it easier to view.
        ///       - Decimals: Do we really need these? Perhaps we do but only if we have
        ///         really small values to deal with.
        ///  In any case, we would like to have all numbers to be aligned, ideally by decimal point
        ///  but I'm too lazy to do that now, so maybe a compomise of fixed two decimals would do at the mo.
        /// 
        ///  Hence the choice of {N2} formatting string.
        ///  </remarks>
        public static Func<double, string> GetOpsPerSecFormattingFunc()
        {
            return (opsPerSec) => string.Format(EnvironmentHelper.MainCultureInfo, "{0:N2}", opsPerSec);
        }
    }
}