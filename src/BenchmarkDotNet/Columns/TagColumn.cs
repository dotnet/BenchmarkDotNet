using System;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class TagColumn : IColumn
    {
        private readonly Func<string, string> getTag;

        public string Id { get; }
        public string ColumnName { get; }

        public TagColumn(string columnName, Func<string, string> getTag)
        {
            this.getTag = getTag;
            ColumnName = columnName;
            Id = nameof(TagColumn) + "." + ColumnName;
        }

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => getTag(benchmarkCase.Descriptor.WorkloadMethod.Name);

        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public int PriorityInCategory => 0;
        public bool IsNumeric => false;
        public UnitType UnitType => UnitType.Dimensionless;
        public string Legend => $"Custom '{ColumnName}' tag column";
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
        public override string ToString() => ColumnName;
    }
}