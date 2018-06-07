using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    public class Q1ColumnAttribute : ColumnConfigBaseAttribute
    {
        public Q1ColumnAttribute() : base(StatisticColumn.Q1)
        {
        }
    }
}