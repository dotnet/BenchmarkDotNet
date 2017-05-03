using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes.Columns
{
    public class MinColumnAttribute : ColumnConfigBaseAttribute
    {
        public MinColumnAttribute() : base(StatisticColumn.Min)
        {
        }
    }
}