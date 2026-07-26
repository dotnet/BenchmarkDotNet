using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;
using Perfolizer.Metrology;
using Pragmastat.Metrology;

namespace BenchmarkDotNet.Columns
{
    public class MetricColumn : IColumn
    {
        internal const string UnknownRepresentation = "?";

        private readonly IMetricDescriptor descriptor;

        public MetricColumn(IMetricDescriptor metricDescriptor) => descriptor = metricDescriptor;

        public string Id => descriptor.Id;
        public string ColumnName => descriptor.DisplayName;
        public string Legend => descriptor.Legend;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Metric;
        public int PriorityInCategory => descriptor.PriorityInCategory;
        public bool IsNumeric => true;
        public UnitType UnitType => descriptor.UnitType;

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

        public bool IsAvailable(Summary summary) => summary.Reports.Any(report =>
            report.Metrics.TryGetValue(descriptor.Id, out var metric)
            && metric.Descriptor.GetIsAvailable(metric));

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, SummaryStyle.Default);

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
        {
            if (!summary.HasReport(benchmarkCase) || !summary[benchmarkCase]!.Metrics.TryGetValue(descriptor.Id, out var metric))
                return "NA";
            if (double.IsNaN(metric.Value))
                return UnknownRepresentation;
            if (metric.Value == 0.0 && !style.PrintZeroValuesInContent)
                return "-";

            var cultureInfo = summary.GetCultureInfo();

            bool printUnits = style.PrintUnitsInContent || style.PrintUnitsInHeader;
            string numberFormat = descriptor.NumberFormat;

            if (printUnits && descriptor.UnitType == UnitType.CodeSize)
            {
                var measurement = SizeValue.FromBytes((long)metric.Value).ToMeasurement(style.CodeSizeUnit);
                return UnitHelper.Format(measurement, numberFormat, cultureInfo, style.PrintUnitsInContent);
            }
            if (printUnits && descriptor.UnitType == UnitType.Size)
            {
                var measurement = SizeValue.FromBytes((long)metric.Value).ToMeasurement(style.SizeUnit);
                return UnitHelper.Format(measurement, numberFormat, cultureInfo, style.PrintUnitsInContent);
            }
            if (printUnits && descriptor.UnitType == UnitType.Time)
            {
                if (numberFormat.IsBlank())
                    numberFormat = "N4";
                var measurement = TimeInterval.FromNanoseconds(metric.Value).ToMeasurement(style.TimeUnit);
                return UnitHelper.Format(measurement, numberFormat, cultureInfo, style.PrintUnitsInContent);
            }

            return metric.Value.ToString(numberFormat, cultureInfo);
        }

        public override string ToString() => descriptor.DisplayName;
    }
}