using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class MetricColumn : IColumn
    {
        public string Legend { get; }
        
        private string UniqueMetricName { get; }

        public MetricColumn(Metric metricInstance)
        {
            UniqueMetricName = metricInstance.UniqueName;
            Legend = metricInstance.Legend;
        }

        public string Id => nameof(MetricColumn) + "." + UniqueMetricName;
        public string ColumnName => UniqueMetricName;
        public bool AlwaysShow => false;
        public ColumnCategory Category => ColumnCategory.Metric;
        public int PriorityInCategory => 0;
        public bool IsNumeric => true;
        public UnitType UnitType => UnitType.Dimensionless;

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

        public bool IsAvailable(Summary summary) => summary.Reports.Any(report => report.Metrics.ContainsKey(UniqueMetricName));

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, SummaryStyle.Default);
        
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, ISummaryStyle style) 
        {
            if (!summary.HasReport(benchmarkCase) || !summary[benchmarkCase].Metrics.TryGetValue(UniqueMetricName, out Metric metric))
                return "-";

            if (style.PrintUnitsInContent && metric.UnitType == UnitType.Size)
                return ((long)metric.Value).ToSizeStr(style.SizeUnit, 1, style.PrintUnitsInContent);
            if (style.PrintUnitsInContent && metric.UnitType == UnitType.Time)
                return metric.Value.ToTimeStr(style.TimeUnit, 1, style.PrintUnitsInContent);

            return metric.Value.ToStr(metric.NumberFormat);
        }

        public override string ToString() => UniqueMetricName;
    }
}