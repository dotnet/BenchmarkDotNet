using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    public class StdErrorColumnAttribute : ColumnConfigBaseAttribute
    {
        public StdErrorColumnAttribute() : base(StatisticColumn.StdErr)
        {
        }
    }
}