using BenchmarkDotNet.Columns;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    public class StdErrorColumnAttribute() : ColumnConfigBaseAttribute(StatisticColumn.StdErr) { }
}