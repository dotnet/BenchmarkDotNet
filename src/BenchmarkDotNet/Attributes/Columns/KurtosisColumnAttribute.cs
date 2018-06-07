using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    public class KurtosisColumnAttribute : ColumnConfigBaseAttribute
    {
        public KurtosisColumnAttribute() : base(StatisticColumn.Kurtosis)
        {
        }
    }
}