using BenchmarkDotNet.Columns;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    public class IterationsColumnAttribute : ColumnConfigBaseAttribute
    {
        public IterationsColumnAttribute() : base(StatisticColumn.Iterations) { }
    }
}