using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Mathematics;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    public class ConfidenceIntervalErrorColumnAttribute : ColumnConfigBaseAttribute
    {
        public ConfidenceIntervalErrorColumnAttribute(ConfidenceLevel level = ConfidenceLevel.L999) : base(StatisticColumn.CiError(level))
        {
        }
    }
}