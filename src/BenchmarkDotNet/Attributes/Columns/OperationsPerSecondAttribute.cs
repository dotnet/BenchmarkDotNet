using BenchmarkDotNet.Columns;

namespace BenchmarkDotNet.Attributes
{
    public class OperationsPerSecondAttribute : ColumnConfigBaseAttribute
    {
        public OperationsPerSecondAttribute() : base(StatisticColumn.OperationsPerSecond) { }
    }
}
