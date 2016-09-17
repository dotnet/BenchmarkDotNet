using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes.Columns
{
    public class StdErrorColumnAttribute : ColumnConfigBaseAttribute
    {
        public StdErrorColumnAttribute() : base(StatisticColumn.StdErr)
        {
        }
    }
}