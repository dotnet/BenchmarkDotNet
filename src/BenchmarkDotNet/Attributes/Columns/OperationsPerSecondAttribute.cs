using BenchmarkDotNet.Columns;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    public class OperationsPerSecondAttribute : ColumnConfigBaseAttribute
    {
        public OperationsPerSecondAttribute() : base(StatisticColumn.OperationsPerSecond) { }
    }
}
