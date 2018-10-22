using BenchmarkDotNet.Columns;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    public class StdErrorColumnAttribute : ColumnConfigBaseAttribute
    {
        public StdErrorColumnAttribute() : base(StatisticColumn.StdErr)
        {
        }
    }
}