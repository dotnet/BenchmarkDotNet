using BenchmarkDotNet.Columns;
using System.Linq;

namespace BenchmarkDotNet.Reports
{
    public static class SummaryExtensions
    {
        public static IColumn[] GetColumns(this Summary summary) =>
            summary.Config.
                GetColumnProviders().
                SelectMany(provider => provider.GetColumns(summary)).
                Where(column => column.IsAvailable(summary)).
                OrderBy(column => column.Category).
                ToArray();
    }
}