using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes.Columns
{
    public class MaxColumnAttribute : ColumnConfigBaseAttribute
    {
        public MaxColumnAttribute() : base(StatisticColumn.Max)
        {
        }
    }
}