using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
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

        public DiffKind Kind { get; set; }

        private BaselineDiffColumn(DiffKind kind)
        {
            Kind = kind;
        }

        public string ColumnName => Kind.ToString();

        public string GetValue(Summary summary, Benchmark benchmark)
        {
            var baselineBenchmark = summary.Benchmarks.
                Where(b => b.Job.GetFullInfo() == benchmark.Job.GetFullInfo()).
                Where(b => b.Parameters.FullInfo == benchmark.Parameters.FullInfo).
                FirstOrDefault(b => b.Target.Baseline);
            if (baselineBenchmark == null)
                return "?";

            var baselineMedian = summary[baselineBenchmark].ResultStatistics.Median;
            var currentMedian = summary[benchmark].ResultStatistics.Median;

            switch (Kind)
            {
                case DiffKind.Delta:
                    if (benchmark.Target.Baseline)
                        return "Baseline";
                    var diff = (currentMedian - baselineMedian)/baselineMedian*100.0;
                    return diff.ToStr("N1") + "%";
                case DiffKind.Scaled:
                    var scale = currentMedian/baselineMedian;
                    return scale.ToStr("N2");
                default:
                    return "?";
            }
        }

        public bool IsAvailable(Summary summary) => summary.Benchmarks.Any(b => b.Target.Baseline);
        public bool AlwaysShow => true;
        public override string ToString() => ColumnName;
    }
}