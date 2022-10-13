using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Columns
{
    public class BaselineColumn : IColumn
    {
        [PublicAPI] public static readonly IColumn Default = new BaselineColumn();

        public string Id => nameof(BaselineColumn);
        public string ColumnName => Column.Baseline;

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => summary.IsBaseline(benchmarkCase) ? "Yes" : "No";
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
        public bool IsAvailable(Summary summary) => true;

        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Meta;
        public int PriorityInCategory => 2;
        public bool IsNumeric => false;
        public UnitType UnitType => UnitType.Dimensionless;
        public string Legend => "";
    }
}