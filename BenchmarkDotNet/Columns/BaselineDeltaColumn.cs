using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class BaselineDeltaColumn : IColumn
    {
        public static readonly IColumn Default = new BaselineDeltaColumn();

        public string ColumnName => "Delta";

        public string GetValue(Summary summary, Benchmark benchmark)
        {
            if (benchmark.Target.Baseline)
                return "Baseline";

            var baselineBenchmark = summary.Benchmarks.
                Where(b => b.Job.GetFullInfo() == benchmark.Job.GetFullInfo()).
                Where(b => b.Parameters.FullInfo == benchmark.Parameters.FullInfo).
                FirstOrDefault(b => b.Target.Baseline);
            if (baselineBenchmark == null)
                return "?";

            var baselineMedian = summary.Reports[baselineBenchmark].ResultStatistics.Median;
            var currentMedian = summary.Reports[benchmark].ResultStatistics.Median;
            var diff = (currentMedian - baselineMedian) / baselineMedian * 100.0;

            return diff.ToStr("0.0") + "%";
        }

        public bool IsAvailable(Summary summary) => summary.Benchmarks.Any(b => b.Target.Baseline);
        public bool AlwaysShow => true;
        public override string ToString() => ColumnName;
    }
}
