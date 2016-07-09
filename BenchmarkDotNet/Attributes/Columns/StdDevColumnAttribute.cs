using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes.Columns
{
    public class StdDevColumnAttribute : ColumnConfigBaseAttribute
    {
        public StdDevColumnAttribute() : base(StatisticColumn.StdDev)
        {
        }
    }
}