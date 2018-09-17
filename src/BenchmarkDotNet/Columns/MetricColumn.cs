using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class MetricColumn : IColumn
    {
        private readonly IMetricDescriptor descriptor;

        public MetricColumn(IMetricDescriptor metricDescriptor) => descriptor = metricDescriptor;

        public string Id => descriptor.Id;
        public string ColumnName => descriptor.DisplayName;
        public string Legend => descriptor.Legend;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Metric;
        public int PriorityInCategory => 0;
        public bool IsNumeric => true;
        public UnitType UnitType => descriptor.UnitType;

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

        public bool IsAvailable(Summary summary) => summary.Reports.Any(report => report.Metrics.ContainsKey(descriptor.Id));

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, SummaryStyle.Default);
        
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, ISummaryStyle style) 
        {
            if (!summary.HasReport(benchmarkCase) || !summary[benchmarkCase].Metrics.TryGetValue(descriptor.Id, out Metric metric) || metric.Value == 0.0)
                return "-";

            if (style.PrintUnitsInContent && descriptor.UnitType == UnitType.Size)
                return ((long)metric.Value).ToSizeStr(style.SizeUnit, 1, style.PrintUnitsInContent);
            if (style.PrintUnitsInContent && descriptor.UnitType == UnitType.Time)
                return metric.Value.ToTimeStr(style.TimeUnit, 1, style.PrintUnitsInContent);

            return metric.Value.ToStr(descriptor.NumberFormat);
        }

        public override string ToString() => descriptor.DisplayName;
    }
}