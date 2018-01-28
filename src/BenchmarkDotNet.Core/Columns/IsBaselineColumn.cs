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

        public string GetValue(Summary summary, Benchmark benchmark) => benchmark.IsBaseline() ? "Yes" : "No";
        public string GetValue(Summary summary, Benchmark benchmark, ISummaryStyle style) => GetValue(summary, benchmark);
        public bool IsDefault(Summary summary, Benchmark benchmark) => false;
        public bool IsAvailable(Summary summary) => true;

        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Meta;
        public int PriorityInCategory => 2;
        public bool IsNumeric => false;
        public UnitType UnitType => UnitType.Dimensionless;
        public string Legend => "";
    }
}