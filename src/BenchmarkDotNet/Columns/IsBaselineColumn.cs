using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Columns
{
    public class IsBaselineColumn: IColumn
    {
        [PublicAPI] public static readonly IColumn Default = new IsBaselineColumn();

        public string Id => nameof(IsBaselineColumn);
        public string ColumnName => "IsBaseline";

        public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => benchmarkCase.IsBaseline() ? "Yes" : "No";
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, ISummaryStyle style) => GetValue(summary, benchmarkCase);
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