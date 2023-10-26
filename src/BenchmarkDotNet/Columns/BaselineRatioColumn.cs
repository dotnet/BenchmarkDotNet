using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class BaselineRatioColumn : BaselineCustomColumn
    {
        public enum RatioMetric
        {
            Mean,
            StdDev
        }

        public static readonly IColumn RatioMean = new BaselineRatioColumn(RatioMetric.Mean);
        public static readonly IColumn RatioStdDev = new BaselineRatioColumn(RatioMetric.StdDev);

        public RatioMetric Metric { get; }

        private BaselineRatioColumn(RatioMetric metric)
        {
            Metric = metric;
        }

        public override string Id => nameof(BaselineRatioColumn) + "." + Metric;

        public override string ColumnName
        {
            get
            {
                switch (Metric)
                {
                    case RatioMetric.Mean:
                        return Column.Ratio;
                    case RatioMetric.StdDev:
                        return Column.RatioSD;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        public override string GetValue(Summary summary, BenchmarkCase benchmarkCase, Statistics baseline, IReadOnlyDictionary<string, Metric> baselineMetrics,
            Statistics current, IReadOnlyDictionary<string, Metric> currentMetrics, bool isBaseline)
        {
            var ratio = GetRatioStatistics(current, baseline);
            if (ratio == null)
                return "NA";
            var invertedRatio = GetRatioStatistics(baseline, current);

            var cultureInfo = summary.GetCultureInfo();
            var ratioStyle = summary?.Style?.RatioStyle ?? RatioStyle.Value;

            switch (Metric)
            {
                case RatioMetric.Mean:
                {
                    bool advancedPrecision = IsNonBaselinesPrecise(summary, baseline, benchmarkCase);
                    switch (ratioStyle)
                    {
                        case RatioStyle.Value:
                            return ratio.Mean.ToString(advancedPrecision ? "N3" : "N2", cultureInfo);
                        case RatioStyle.Percentage:
                            return isBaseline
                                ? "baseline"
                                : ratio.Mean >= 1.0
                                    ? "+" + ((ratio.Mean - 1.0) * 100).ToString(advancedPrecision ? "N1" : "N0", cultureInfo) + "%"
                                    : "-" + ((1.0 - ratio.Mean) * 100).ToString(advancedPrecision ? "N1" : "N0", cultureInfo) + "%";
                        case RatioStyle.Trend:
                            return isBaseline
                                ? "baseline"
                                : ratio.Mean >= 1.0
                                    ? ratio.Mean.ToString(advancedPrecision ? "N3" : "N2", cultureInfo) + "x slower"
                                    : invertedRatio == null
                                        ? "NA"
                                        : invertedRatio.Mean.ToString(advancedPrecision ? "N3" : "N2", cultureInfo) + "x faster";
                        default:
                            throw new ArgumentOutOfRangeException(nameof(summary), ratioStyle, "RatioStyle is not supported");
                    }
                }
                case RatioMetric.StdDev:
                {
                    switch (ratioStyle)
                    {
                        case RatioStyle.Value:
                            return ratio.StandardDeviation.ToString("N2", cultureInfo);
                        case RatioStyle.Percentage:
                            return isBaseline
                                ? ""
                                : Math.Abs(ratio.Mean) < 1e-9
                                    ? "NA"
                                    : (100 * ratio.StandardDeviation / ratio.Mean).ToString("N1", cultureInfo) + "%";
                        case RatioStyle.Trend:
                            return isBaseline
                                ? ""
                                : ratio.Mean >= 1.0
                                    ? ratio.StandardDeviation.ToString("N2", cultureInfo) + "x"
                                    : invertedRatio == null
                                        ? "NA"
                                        : invertedRatio.StandardDeviation.ToString("N2", cultureInfo) + "x";
                        default:
                            throw new ArgumentOutOfRangeException(nameof(summary), ratioStyle, "RatioStyle is not supported");
                    }
                }
                default:
                    throw new NotSupportedException();
            }
        }

        private static bool IsNonBaselinesPrecise(Summary summary, Statistics baselineStat, BenchmarkCase benchmarkCase)
        {
            string logicalGroupKey = summary.GetLogicalGroupKey(benchmarkCase);
            var nonBaselines = summary.GetNonBaselines(logicalGroupKey);
            return nonBaselines.Any(x => GetRatioStatistics(summary[x].ResultStatistics, baselineStat)?.Mean < 0.01);
        }

        private static Statistics? GetRatioStatistics(Statistics? current, Statistics? baseline)
        {
            if (current == null || current.N < 1)
                return null;
            if (baseline == null || baseline.N < 1)
                return null;
            try
            {
                return Statistics.Divide(current, baseline);
            }
            catch (DivideByZeroException)
            {
                return null;
            }
        }

        public override int PriorityInCategory => (int) Metric;
        public override bool IsNumeric => true;
        public override UnitType UnitType => UnitType.Dimensionless;

        public override string Legend
        {
            get
            {
                switch (Metric)
                {
                    case RatioMetric.Mean:
                        return "Mean of the ratio distribution ([Current]/[Baseline])";
                    case RatioMetric.StdDev:
                        return "Standard deviation of the ratio distribution ([Current]/[Baseline])";
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Metric));
                }
            }
        }
    }
}