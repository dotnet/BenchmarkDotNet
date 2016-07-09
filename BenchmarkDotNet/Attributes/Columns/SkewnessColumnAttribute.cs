using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes.Columns
{
    public class SkewnessColumnAttribute : ColumnConfigAttribute
    {
        public SkewnessColumnAttribute() : base(StatisticColumn.Skewness)
        {
        }
    }
}