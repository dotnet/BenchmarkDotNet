using BenchmarkDotNet.Columns;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    public class StdDevColumnAttribute : ColumnConfigBaseAttribute
    {
        public StdDevColumnAttribute() : base(StatisticColumn.StdDev)
        {
        }
    }
}