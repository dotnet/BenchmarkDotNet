using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes.Columns
{
    public class AllStatisticsColumnAttribute : ColumnConfigBaseAttribute
    {
        public AllStatisticsColumnAttribute() : base(StatisticColumn.AllStatistics)
        {
        }
    }
}