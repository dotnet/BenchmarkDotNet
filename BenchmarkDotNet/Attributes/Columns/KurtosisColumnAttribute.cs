using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes.Columns
{
    public class KurtosisColumnAttribute : ColumnConfigBaseAttribute
    {
        public KurtosisColumnAttribute() : base(StatisticColumn.Kurtosis)
        {
        }
    }
}