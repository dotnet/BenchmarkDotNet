using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes.Columns
{
    public class KurtosisColumnAttribute : ColumnConfigAttribute
    {
        public KurtosisColumnAttribute() : base(StatisticColumn.Kurtosis)
        {
        }
    }
}