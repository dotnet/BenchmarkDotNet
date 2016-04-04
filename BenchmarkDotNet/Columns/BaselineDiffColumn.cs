using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class BaselineDiffColumn : IColumn
    {
        public enum DiffKind
        {
            Delta,
            Scaled
        }

        public static readonly IColumn Delta = new BaselineDiffColumn(DiffKind.Delta);
        public static readonly IColumn Scaled = new BaselineDiffColumn(DiffKind.Scaled);
        public static readonly IColumn Scaled50 = new BaselineDiffColumn(DiffKind.Scaled, 50);
        public static readonly IColumn Scaled85 = new BaselineDiffColumn(DiffKind.Scaled, 85);
        public static readonly IColumn Scaled95 = new BaselineDiffColumn(DiffKind.Scaled, 95);

        public DiffKind Kind { get; set; }
        public int? Percentile { get; set; }

        private BaselineDiffColumn(DiffKind kind, int? percentile = null)
        {
            Kind = kind;
            Percentile = percentile;
        }

        public string ColumnName => Percentile == null ?
            Kind.ToString() :
            Kind.ToString() + "P" + Percentile?.ToString();

        public string GetValue(Summary summary, Benchmark benchmark)
        {
            var baselineBenchmark = summary.Benchmarks.
                Where(b => b.Job.GetFullInfo() == benchmark.Job.GetFullInfo()).
                Where(b => b.Parameters.FullInfo == benchmark.Parameters.FullInfo).
                FirstOrDefault(b => b.Target.Baseline);
            var invalidResults = baselineBenchmark == null ||
                                 summary[baselineBenchmark] == null ||
                                 summary[baselineBenchmark].ResultStatistics == null ||
                                 summary[benchmark] == null ||
                                 summary[benchmark].ResultStatistics == null;

            if (invalidResults)
                return "?";

            double baselineMetric;
            double currentMetric;

            var resultStatistics = summary[baselineBenchmark].ResultStatistics;
            var statistics = summary[benchmark].ResultStatistics;
            if (Percentile == null)
            {
                baselineMetric = resultStatistics.Median;
                currentMetric = statistics.Median;
            }
            else
            {
                baselineMetric = resultStatistics.Percentiles.Percentile(Percentile.GetValueOrDefault());
                currentMetric = statistics.Percentiles.Percentile(Percentile.GetValueOrDefault());
            }

            if (baselineMetric == 0)
                return "?";

            switch (Kind)
            {
                case DiffKind.Delta:
                    if (benchmark.Target.Baseline)
                        return "Baseline";
                    var diff = (currentMetric - baselineMetric) / baselineMetric * 100.0;
                    return diff.ToStr("N1") + "%";
                case DiffKind.Scaled:
                    var scale = currentMetric / baselineMetric;
                    return scale.ToStr("N2");
                default:
                    return "?";
            }
        }

        public bool IsAvailable(Summary summary) => summary.Benchmarks.Any(b => b.Target.Baseline);
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Statistics;
        public override string ToString() => ColumnName;
    }
}