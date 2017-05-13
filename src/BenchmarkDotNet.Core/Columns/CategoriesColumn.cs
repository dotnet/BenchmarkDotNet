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
        public string ColumnName => "Categories";
        public string GetValue(Summary summary, Benchmark benchmark) => string.Join(",", benchmark.Target.Categories);
        public string GetValue(Summary summary, Benchmark benchmark, ISummaryStyle style) => GetValue(summary, benchmark);
        public bool IsDefault(Summary summary, Benchmark benchmark) => false;
        public bool IsAvailable(Summary summary) => summary.Benchmarks.Any(b => !b.Target.Categories.IsEmpty());
        public bool AlwaysShow => false;
        public ColumnCategory Category => ColumnCategory.Job;
        public int PriorityInCategory => 100;
        public bool IsNumeric => false;
        public UnitType UnitType => UnitType.Dimensionless;
        public string Legend => "All categories of the corresponded method, class, and assembly";
    }
}