﻿using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Reports
{
    public static class SummaryExtensions
    {
        public static IColumn[] GetColumns(this Summary summary)
            => summary
                .BenchmarksCases
                .SelectMany(benchmark => benchmark.Config.GetColumnProviders())
                .Distinct()
                .SelectMany(provider => provider.GetColumns(summary))
                .Where(column => column.IsAvailable(summary))
                .GroupBy(column => column.Id)
                .Select(group => group.First())
                .OrderBy(column => column.Category)
                .ThenBy(column => column.PriorityInCategory)
                .ToArray();

        public static IEnumerable<BenchmarkCase> GetLogicalGroupForBenchmark(this Summary summary, BenchmarkCase benchmarkCase)
        {
            string logicalGroupKey = summary.GetLogicalGroupKey(benchmarkCase);
            return summary.BenchmarksCases.Where(b => summary.GetLogicalGroupKey(b) == logicalGroupKey);
        }
    }
}