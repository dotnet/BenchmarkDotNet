using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    public class StdDevColumnAttribute : ColumnConfigBaseAttribute
    {
        public StdDevColumnAttribute() : base(StatisticColumn.StdDev)
        {
        }
    }
}