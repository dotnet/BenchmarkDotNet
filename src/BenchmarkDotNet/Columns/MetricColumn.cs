using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;
using Perfolizer.Metrology;

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
            if (!summary.HasReport(benchmarkCase) || !summary[benchmarkCase].Metrics.TryGetValue(descriptor.Id, out Metric metric))
                return "NA";
            if (double.IsNaN(metric.Value))
                return UnknownRepresentation;
            if (metric.Value == 0.0 && !style.PrintZeroValuesInContent)
                return "-";

            var cultureInfo = summary.GetCultureInfo();

            bool printUnits = style.PrintUnitsInContent || style.PrintUnitsInHeader;
            var unitPresentation = new UnitPresentation(style.PrintUnitsInContent, minUnitWidth: 0, gap: true);
            string numberFormat = descriptor.NumberFormat;

            if (printUnits && descriptor.UnitType == UnitType.CodeSize)
                return SizeValue.FromBytes((long)metric.Value).ToString(style.CodeSizeUnit, numberFormat, cultureInfo, unitPresentation);
            if (printUnits && descriptor.UnitType == UnitType.Size)
                return SizeValue.FromBytes((long)metric.Value).ToString(style.SizeUnit, numberFormat, cultureInfo, unitPresentation);
            if (printUnits && descriptor.UnitType == UnitType.Time)
            {
                if (numberFormat.IsBlank())
                    numberFormat = "N4";
                return TimeInterval.FromNanoseconds(metric.Value).ToString(style.TimeUnit, numberFormat, cultureInfo, unitPresentation);
            }

            return metric.Value.ToString(numberFormat, cultureInfo);
        }

        public override string ToString() => descriptor.DisplayName;
    }
}