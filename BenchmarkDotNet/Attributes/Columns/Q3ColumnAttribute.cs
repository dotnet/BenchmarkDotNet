using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes.Columns
{
    public class Q3ColumnAttribute : ColumnConfigBaseAttribute
    {
        public Q3ColumnAttribute() : base(StatisticColumn.Q3)
        {
        }
    }
}