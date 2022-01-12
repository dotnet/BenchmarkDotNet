using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class CategoriesColumn : IColumn
    {
        public static readonly IColumn Default = new CategoriesColumn();

        public string Id => nameof(CategoriesColumn);
        public string ColumnName => Column.Categories;
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => string.Join(",", benchmarkCase.Descriptor.Categories);
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
        public bool IsAvailable(Summary summary) => summary.BenchmarksCases.Any(b => !b.Descriptor.Categories.IsEmpty());
        public bool AlwaysShow => false;
        public ColumnCategory Category => ColumnCategory.Job;
        public int PriorityInCategory => 100;
        public bool IsNumeric => false;
        public UnitType UnitType => UnitType.Dimensionless;
        public string Legend => "All categories of the corresponded method, class, and assembly";
    }
}