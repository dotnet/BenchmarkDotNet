using BenchmarkDotNet.Columns;
using JetBrains.Annotations;
using Perfolizer.Mathematics.Common;

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