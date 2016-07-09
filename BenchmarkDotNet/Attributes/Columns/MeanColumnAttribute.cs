using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes.Columns
{
    public class MeanColumnAttribute : ColumnConfigBaseAttribute
    {
        public MeanColumnAttribute() : base(StatisticColumn.Mean)
        {
        }
    }
}