using System.Linq;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class ParamColumn : IColumn
    {
        public string ColumnName { get; }

        public ParamColumn(string columnName)
        {
            ColumnName = columnName;
        }

        public string GetValue(Summary summary, Benchmark benchmark) =>
            benchmark.Parameters.Items.FirstOrDefault(item => item.Name == ColumnName)?.Value.ToString() ?? "?";

        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;
        public override string ToString() => ColumnName;
    }
}