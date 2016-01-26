using System.Linq;
using BenchmarkDotNet.Extensions;
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
            var reports = summary.Reports.Values;
            var benchmarks = summary.Benchmarks;

            double baseline = 0;
            // TODO: Implement params
            //if (item.Item1.Parameters != null)
            //{
            //    var firstMatch =
            //        reports.FirstOrDefault(r => r.Item1.Benchmark.Target.Baseline &&
            //                                    r.Item1.Parameters.IntParam == item.Item1.Parameters.IntParam);
            //    if (firstMatch != null)
            //        baseline = firstMatch.Item2.Mean;
            //}
            //else
            {
                // TODO: repair matching
                var firstMatch = benchmarks.First(b => b.Target.Baseline);
                if (firstMatch != null)
                    baseline = summary.Reports[firstMatch].TargetStatistics.Mean;
            }

            var current = summary.Reports[benchmark].TargetStatistics.Mean;
            double diff = 0;
            if (baseline != 0) // This can happen if we found no matching result
                diff = (current - baseline) / baseline * 100.0;

            return benchmark.Target.Baseline ? "Baseline" : diff.ToStr("0.0") + "%";
        }

        public bool IsAvailable(Summary summary) => summary.Benchmarks.Any(b => b.Target.Baseline);

        public bool AlwaysShow => true;
        public override string ToString() => ColumnName;
    }
}
