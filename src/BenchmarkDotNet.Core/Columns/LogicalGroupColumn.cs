using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Columns
{
    public class LogicalGroupColumn : IColumn
    {
        [PublicAPI] public static readonly IColumn Default = new LogicalGroupColumn();

        public string Id => nameof(LogicalGroupColumn);
        public string ColumnName => "LogicalGroup";

        public string GetValue(Summary summary, Benchmark benchmark) => summary.GetLogicalGroupKey(benchmark);
        public string GetValue(Summary summary, Benchmark benchmark, ISummaryStyle style) => GetValue(summary, benchmark);
        public bool IsDefault(Summary summary, Benchmark benchmark) => false;
        public bool IsAvailable(Summary summary) => true;

        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Meta;
        public int PriorityInCategory => 1;
        public bool IsNumeric => false;
        public UnitType UnitType => UnitType.Dimensionless;
        public string Legend => "";
    }
}