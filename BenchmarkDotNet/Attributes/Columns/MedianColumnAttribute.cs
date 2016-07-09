using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes.Columns
{
    public class MedianColumnAttribute : ColumnConfigBaseAttribute
    {
        public MedianColumnAttribute() : base(StatisticColumn.Median)
        {
        }
    }
}