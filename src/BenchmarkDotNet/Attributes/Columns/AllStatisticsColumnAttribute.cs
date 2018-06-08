using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    public class AllStatisticsColumnAttribute : ColumnConfigBaseAttribute
    {
        public AllStatisticsColumnAttribute() : base(StatisticColumn.AllStatistics)
        {
        }
    }
}