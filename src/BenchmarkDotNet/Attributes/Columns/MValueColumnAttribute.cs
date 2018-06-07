using BenchmarkDotNet.Columns;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Attributes
{
    /// <summary>
    /// Prints mvalue.
    /// See http://www.brendangregg.com/FrequencyTrails/modes.html
    /// </summary>
    [PublicAPI]
    public class MValueColumnAttribute: ColumnConfigBaseAttribute
    {
        public MValueColumnAttribute() : base(StatisticColumn.MValue)
        {
        }
    }
}