using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class BaselineAllocationRatioColumn : BaselineCustomColumn
    {
        public override string Id => nameof(BaselineAllocationRatioColumn);

        public override string ColumnName => Column.AllocRatio;

        public static readonly IColumn RatioMean = new BaselineAllocationRatioColumn();

        private BaselineAllocationRatioColumn() { }

        public override string GetValue(Summary summary, BenchmarkCase benchmarkCase, Statistics baseline, IReadOnlyDictionary<string, Metric> baselineMetrics,
            Statistics current, IReadOnlyDictionary<string, Metric> currentMetrics, bool isBaseline)
        {
            double? ratio = GetAllocationRatio(currentMetrics, baselineMetrics);
            double? invertedRatio = GetAllocationRatio(baselineMetrics, currentMetrics);

            if (ratio == null)
                return "NA";

            var cultureInfo = summary.GetCultureInfo();
            var ratioStyle = summary?.Style?.RatioStyle ?? RatioStyle.Value;

            bool advancedPrecision = IsNonBaselinesPrecise(summary, baselineMetrics, benchmarkCase);
            switch (ratioStyle)
            {
                case RatioStyle.Value:
                    return ratio.Value.ToString(advancedPrecision ? "N3" : "N2", cultureInfo);
                case RatioStyle.Percentage:
                    return isBaseline
                        ? ""
                        : ratio.Value >= 1.0
                            ? "+" + ((ratio.Value - 1.0) * 100).ToString(advancedPrecision ? "N1" : "N0", cultureInfo) + "%"
                            : "-" + ((1.0 - ratio.Value) * 100).ToString(advancedPrecision ? "N1" : "N0", cultureInfo) + "%";
                case RatioStyle.Trend:
                    return isBaseline
                        ? ""
                        : ratio.Value >= 1.0
                            ? ratio.Value.ToString(advancedPrecision ? "N3" : "N2", cultureInfo) + "x more"
                            : invertedRatio == null
                                ? "NA"
                                : invertedRatio.Value.ToString(advancedPrecision ? "N3" : "N2", cultureInfo) + "x less";
                default:
                    throw new ArgumentOutOfRangeException(nameof(summary), ratioStyle, "RatioStyle is not supported");
            }
        }

        private static bool IsNonBaselinesPrecise(Summary summary, IReadOnlyDictionary<string, Metric> baselineMetric, BenchmarkCase benchmarkCase)
        {
            string logicalGroupKey = summary.GetLogicalGroupKey(benchmarkCase);
            var nonBaselines = summary.GetNonBaselines(logicalGroupKey);
            return nonBaselines.Any(c => GetAllocationRatio(summary[c].Metrics, baselineMetric) is > 0 and < 0.01);
        }

        private static double? GetAllocationRatio(
            IReadOnlyDictionary<string, Metric>? current,
            IReadOnlyDictionary<string, Metric>? baseline)
        {
            double? currentBytes = GetAllocatedBytes(current);
            double? baselineBytes = GetAllocatedBytes(baseline);

            if (currentBytes == null || baselineBytes == null)
                return null;

            if (baselineBytes == 0)
                return null;

            return currentBytes / baselineBytes;
        }

        private static double? GetAllocatedBytes(IReadOnlyDictionary<string, Metric>? metrics)
        {
            var metric = metrics?.Values.FirstOrDefault(m => m.Descriptor is AllocatedMemoryMetricDescriptor);
            return metric?.Value;
        }

        public override ColumnCategory Category => ColumnCategory.Metric; //it should be displayed after Allocated column
        public override int PriorityInCategory => AllocatedMemoryMetricDescriptor.Instance.PriorityInCategory + 1;
        public override bool IsNumeric => true;
        public override UnitType UnitType => UnitType.Dimensionless;
        public override string Legend => "Allocated memory ratio distribution ([Current]/[Baseline])";
    }
}