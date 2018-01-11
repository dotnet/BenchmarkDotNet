using System.Collections.Generic;
using BenchmarkDotNet.Columns;
using System.Linq;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Reports
{
    public static class SummaryExtensions
    {
        public static IColumn[] GetColumns(this Summary summary) =>
            summary.Config.
                GetColumnProviders().
                SelectMany(provider => provider.GetColumns(summary)).
                Where(column => column.IsAvailable(summary)).
                GroupBy(column => column.Id).
                Select(group => group.First()).
                OrderBy(column => column.Category).
                ThenBy(column => column.PriorityInCategory).
                ToArray();

        public static IEnumerable<Benchmark> GetLogicalGroupForBenchmark(this Summary summary, Benchmark benchmark)
        {
            string logicalGroupKey = summary.GetLogicalGroupKey(benchmark);
            return summary.Benchmarks.Where(b => summary.GetLogicalGroupKey(b) == logicalGroupKey);
        }
    }
}