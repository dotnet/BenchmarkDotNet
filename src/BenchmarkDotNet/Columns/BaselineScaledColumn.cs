using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Columns
{
    [Obsolete("Use BaselineRatioColumn"), PublicAPI]
    public class BaselineScaledColumn : BaselineCustomColumn
    {
        public enum ScaledKind
        {
            Mean
        }

        public static readonly IColumn Scaled = new BaselineScaledColumn(ScaledKind.Mean);

        public ScaledKind Kind { get; }

        private BaselineScaledColumn(ScaledKind kind)
        {
            Kind = kind;
        }

        public override string Id => nameof(BaselineScaledColumn) + "." + Kind;

        public override string ColumnName
        {
            get
            {
                switch (Kind)
                {
                    case ScaledKind.Mean:
                        return "Scaled";
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        public override string GetValue(Summary summary, BenchmarkCase benchmarkCase, Statistics baseline, IReadOnlyDictionary<string, Metric> baselineMetrics,
            Statistics current, IReadOnlyDictionary<string, Metric> currentMetrics, bool isBaseline)
        {
            double mean = isBaseline ? 1 : Statistics.DivMean(current, baseline);

            var cultureInfo = summary.GetCultureInfo();
            switch (Kind)
            {
                case ScaledKind.Mean:
                    return IsNonBaselinesPrecise(summary, baseline, benchmarkCase) ? mean.ToString("N3", cultureInfo) : mean.ToString("N2", cultureInfo);
                default:
                    throw new NotSupportedException();
            }
        }

        private static bool IsNonBaselinesPrecise(Summary summary, Statistics baselineStat, BenchmarkCase benchmarkCase)
        {
            string logicalGroupKey = summary.GetLogicalGroupKey(benchmarkCase);
            var nonBaselines = summary.GetNonBaselines(logicalGroupKey);

            return nonBaselines.Any(x => Statistics.DivMean(summary[x].ResultStatistics, baselineStat) < 0.01);
        }

        public override int PriorityInCategory => (int) Kind;
        public override bool IsNumeric => true;
        public override UnitType UnitType => UnitType.Dimensionless;

        public override string Legend
        {
            get
            {
                switch (Kind)
                {
                    case ScaledKind.Mean:
                        return "Mean(CurrentBenchmark) / Mean(BaselineBenchmark)";
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Kind));
                }
            }
        }
    }
}