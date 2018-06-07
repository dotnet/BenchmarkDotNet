using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    public class MaxColumnAttribute : ColumnConfigBaseAttribute
    {
        public MaxColumnAttribute() : base(StatisticColumn.Max)
        {
        }
    }
}