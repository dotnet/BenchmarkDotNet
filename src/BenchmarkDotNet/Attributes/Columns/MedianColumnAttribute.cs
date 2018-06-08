using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    public class MedianColumnAttribute : ColumnConfigBaseAttribute
    {
        public MedianColumnAttribute() : base(StatisticColumn.Median)
        {
        }
    }
}