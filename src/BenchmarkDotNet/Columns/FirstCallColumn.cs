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
        public static readonly IColumn Default = new FirstCallColumn();
        
        public string Id => nameof(FirstCallColumn);

        public string ColumnName => "FirstCall";

        public bool AlwaysShow => true;

        public ColumnCategory Category => ColumnCategory.Statistics;

        public int PriorityInCategory => int.MinValue;

        public bool IsNumeric => true;

        public UnitType UnitType => UnitType.Time;

        public string Legend => "Execution time of the first call";

        public List<double> GetAllValues(Summary summary, SummaryStyle style)
            => summary.Reports
                .Where(HasAnyRealWorkloads)
                .Select(r => GetFirstCall(r).Nanoseconds)
                .Where(v => !double.IsNaN(v) && !double.IsInfinity(v))
                .ToList();

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
        {
            if (!summary.HasReport(benchmarkCase) || !HasAnyRealWorkloads(summary[benchmarkCase]))
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

        public bool IsAvailable(Summary summary) => summary.Reports.Any(HasAnyRealWorkloads);

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

        private static bool HasAnyRealWorkloads(BenchmarkReport report)
            => report.AllMeasurements.Any(m => m.IterationMode == IterationMode.Workload);

        private static Measurement GetFirstCall(BenchmarkReport report)
            => report.AllMeasurements
                .Where(m => m.IterationMode == IterationMode.Workload)
                .OrderBy(m => m.IterationMode) // Jitting, Pilot, Warmup, Workload
                .ThenBy(m => m.IterationIndex)
                .First();
    }
}
