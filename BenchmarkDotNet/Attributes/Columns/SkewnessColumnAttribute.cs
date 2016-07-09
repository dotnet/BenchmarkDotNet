using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes.Columns
{
    public class SkewnessColumnAttribute : ColumnConfigBaseAttribute
    {
        public SkewnessColumnAttribute() : base(StatisticColumn.Skewness)
        {
        }
    }
}