using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    public class MinColumnAttribute : ColumnConfigBaseAttribute
    {
        public MinColumnAttribute() : base(StatisticColumn.Min)
        {
        }
    }
}