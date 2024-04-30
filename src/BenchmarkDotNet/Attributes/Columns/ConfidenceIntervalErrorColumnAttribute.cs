using BenchmarkDotNet.Columns;
using JetBrains.Annotations;
using Perfolizer.Mathematics.Common;

namespace BenchmarkDotNet.Attributes
{
    [PublicAPI]
    public class ConfidenceIntervalErrorColumnAttribute : ColumnConfigBaseAttribute
    {
        public ConfidenceIntervalErrorColumnAttribute() : base(StatisticColumn.CiError(ConfidenceLevel.L999)) { }
        public ConfidenceIntervalErrorColumnAttribute(ConfidenceLevel level) : base(StatisticColumn.CiError(level)) { }
    }
}