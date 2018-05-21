﻿using System;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class BaselineScaledColumn : IColumn
    {
        public enum DiffKind
        {
            Mean,
            StdDev,
            WelchTTestPValue
        }

        public static readonly IColumn Scaled = new BaselineScaledColumn(DiffKind.Mean);
        public static readonly IColumn ScaledStdDev = new BaselineScaledColumn(DiffKind.StdDev);
        public static readonly IColumn WelchTTestPValue = new BaselineScaledColumn(DiffKind.WelchTTestPValue);

        public DiffKind Kind { get; }

        private BaselineScaledColumn(DiffKind kind)
        {
            Kind = kind;
        }

        public string Id => nameof(BaselineScaledColumn) + "." + Kind;

        public string ColumnName
        {
            get
            {
                switch (Kind)
                {
                    case DiffKind.Mean:
                        return "Scaled";
                    case DiffKind.StdDev:
                        return "ScaledSD";
                    case DiffKind.WelchTTestPValue:
                        return "t-test p-value";
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        public string GetValue(Summary summary, Benchmark benchmark)
        {
            string logicalGroupKey = summary.GetLogicalGroupKey(benchmark);
            var baseline = summary.Benchmarks
                .Where(b => summary.GetLogicalGroupKey(b) == logicalGroupKey)
                .FirstOrDefault(b => b.IsBaseline());
            bool invalidResults = baseline == null ||
                                 summary[baseline] == null ||
                                 summary[baseline].ResultStatistics == null ||
                                 !summary[baseline].ResultStatistics.CanBeInverted() ||
                                 summary[benchmark] == null ||
                                 summary[benchmark].ResultStatistics == null;

            if (invalidResults)
                return "?";

            var baselineStat = summary[baseline].ResultStatistics;
            var targetStat = summary[benchmark].ResultStatistics;

            double mean = benchmark.IsBaseline() ? 1 : Statistics.DivMean(targetStat, baselineStat);
            double stdDev = benchmark.IsBaseline() ? 0 : Math.Sqrt(Statistics.DivVariance(targetStat, baselineStat));

            switch (Kind)
            {
                case DiffKind.Mean:
                    return IsNonBaselinesPrecise(summary, baselineStat, benchmark) ? mean.ToStr("N3") : mean.ToStr("N2");
                case DiffKind.StdDev:
                    return stdDev.ToStr("N2");
                case DiffKind.WelchTTestPValue:
                {
                    if (baselineStat.N < 2 || targetStat.N < 2)
                        return "NA";
                    double pvalue = WelchTTest.Calc(baselineStat, targetStat).PValue;
                    return pvalue > 0.0001 || pvalue < 1e-9 ? pvalue.ToStr("N4") : pvalue.ToStr("e2");
                }
                default:
                    throw new NotSupportedException();
            }
        }

        public bool IsNonBaselinesPrecise(Summary summary, Statistics baselineStat, Benchmark benchmark)
        {
            string logicalGroupKey = summary.GetLogicalGroupKey(benchmark);
            var nonBaselines = summary.Benchmarks
                        .Where(b => summary.GetLogicalGroupKey(b) == logicalGroupKey)
                        .Where(b => !b.IsBaseline());

            return nonBaselines.Any(x => Statistics.DivMean(summary[x].ResultStatistics, baselineStat) < 0.01);
        }

        public bool IsAvailable(Summary summary) => summary.Benchmarks.Any(b => b.IsBaseline());
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Baseline;
        public int PriorityInCategory => (int) Kind;
        public bool IsNumeric => true;
        public UnitType UnitType => UnitType.Dimensionless;
        public string GetValue(Summary summary, Benchmark benchmark, ISummaryStyle style) => GetValue(summary, benchmark);
        public override string ToString() => ColumnName;
        public bool IsDefault(Summary summary, Benchmark benchmark) => false;

        public string Legend
        {
            get
            {
                switch (Kind)
                {
                    case DiffKind.Mean:
                        return "Mean(CurrentBenchmark) / Mean(BaselineBenchmark)";
                    case DiffKind.StdDev:
                        return "Standard deviation of ratio of distribution of [CurrentBenchmark] and [BaselineBenchmark]";
                    case DiffKind.WelchTTestPValue:
                        return "p-value for Welch's t-test of [CurrentbBenchmark] and [BaselineBenchmark]";
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Kind));
                }
            }
        }
    }
}
