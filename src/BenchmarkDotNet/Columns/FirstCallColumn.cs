using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class FirstCallColumn : IStatisticColumn
    {
        public string Id => nameof(FirstCallColumn);

        public string ColumnName => "FirstCall";

        public bool AlwaysShow => true;

        public ColumnCategory Category => ColumnCategory.Statistics;

        public int PriorityInCategory => int.MinValue;

        public bool IsNumeric => true;

        public UnitType UnitType => UnitType.Time;

        public string Legend => "Execution time of the first call (Jitting included)";

        public List<double> GetAllValues(Summary summary, SummaryStyle style)
            => summary.Reports
                .Where(HasSingleCall)
                .Select(r => GetFirstCall(r).Nanoseconds)
                .Where(v => !double.IsNaN(v) && !double.IsInfinity(v))
                .Select(v => v / style.TimeUnit.NanosecondAmount)
                .ToList();

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            if (!summary.HasReport(benchmarkCase) || !HasSingleCall(summary[benchmarkCase]))
                return "-";

            var measurement = GetFirstCall(summary[benchmarkCase]);

            var style = summary.Style;
            int precision = summary.DisplayPrecisionManager.GetPrecision(style, this, null);
            string format = "N" + precision;

            return TimeInterval.FromNanoseconds(measurement.Nanoseconds)
                .ToString(
                    style.TimeUnit,
                    style.CultureInfo,
                    format,
                    UnitPresentation.FromVisibility(style.PrintUnitsInContent));
        }

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);

        public bool IsAvailable(Summary summary) => summary.Reports.Any(HasSingleCall);

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

        private static bool HasSingleCall(BenchmarkReport report)
            => report.AllMeasurements.Any(m => m.IterationMode == IterationMode.Workload && m.Operations == 1);

        private static Measurement GetFirstCall(BenchmarkReport report)
            => report.AllMeasurements
                .Where(m => m.IterationMode == IterationMode.Workload && m.Operations == 1)
                .OrderBy(m => m.IterationMode) // Jitting, Pilot, Warmup, Workload
                .ThenBy(m => m.IterationIndex)
                .First();
    }
}
