using BenchmarkDotNet.Columns;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    public class AllStatisticsColumnAttribute : ColumnConfigBaseAttribute
    {
        public AllStatisticsColumnAttribute() : base(StatisticColumn.AllStatistics)
        {
        }
    }
}