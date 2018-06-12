using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    public class MeanColumnAttribute : ColumnConfigBaseAttribute
    {
        public MeanColumnAttribute() : base(StatisticColumn.Mean)
        {
        }
    }
}