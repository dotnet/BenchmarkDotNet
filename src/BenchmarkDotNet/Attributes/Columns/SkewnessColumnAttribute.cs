using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    public class SkewnessColumnAttribute : ColumnConfigBaseAttribute
    {
        public SkewnessColumnAttribute() : base(StatisticColumn.Skewness)
        {
        }
    }
}