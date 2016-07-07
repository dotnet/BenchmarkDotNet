using System;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
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
            StdDev
        }

        public static readonly IColumn Scaled = new BaselineScaledColumn(DiffKind.Mean);
        public static readonly IColumn ScaledStdDev = new BaselineScaledColumn(DiffKind.StdDev);

        public DiffKind Kind { get; set; }

        private BaselineScaledColumn(DiffKind kind)
        {
            Kind = kind;
        }

        public string ColumnName
        {
            get
            {
                switch (Kind)
                {
                    case DiffKind.Mean:
                        return "Scaled";
                    case DiffKind.StdDev:
                        return "Scaled-SD";
                }
                throw new NotSupportedException();
            }
        }

        public string GetValue(Summary summary, Benchmark benchmark)
        {
            var baseline = summary.Benchmarks.
                Where(b => b.Job.GetFullInfo() == benchmark.Job.GetFullInfo()).
                Where(b => b.Parameters.FullInfo == benchmark.Parameters.FullInfo).
                FirstOrDefault(b => b.Target.Baseline);
            var invalidResults = baseline == null ||
                                 summary[baseline] == null ||
                                 summary[baseline].ResultStatistics == null ||
                                 summary[baseline].ResultStatistics.Invert() == null ||
                                 summary[benchmark] == null ||
                                 summary[benchmark].ResultStatistics == null;

            if (invalidResults)
                return "?";

            var baselineStat = summary[baseline].ResultStatistics;
            var targetStat = summary[benchmark].ResultStatistics;

            var mean = benchmark.Target.Baseline ? 1 : Statistics.DivMean(targetStat, baselineStat);
            var stdDev = benchmark.Target.Baseline ? 0 : Math.Sqrt(Statistics.DivVariance(targetStat, baselineStat));

            switch (Kind)
            {
                case DiffKind.Mean:
                    return mean.ToStr("N2");
                case DiffKind.StdDev:
                    return stdDev.ToStr("N2");
                default:
                    throw new NotSupportedException();
            }
        }

        public bool IsAvailable(Summary summary) => summary.Benchmarks.Any(b => b.Target.Baseline);
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Statistics;
        public override string ToString() => ColumnName;
    }
}