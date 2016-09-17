using System;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class TagColumn : IColumn
    {
        private readonly Func<string, string> getTag;

        public string ColumnName { get; }

        public TagColumn(string columnName, Func<string, string> getTag)
        {
            this.getTag = getTag;
            ColumnName = columnName;
        }

        public bool IsDefault(Summary summary, Benchmark benchmark) => false;
        public string GetValue(Summary summary, Benchmark benchmark) => getTag(benchmark.Target.Method.Name);

        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public override string ToString() => ColumnName;
    }
}