using System.Linq;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class ParamColumn : IColumn
    {
        public string Id => nameof(ParamColumn) + "." + ColumnName;
        public string ColumnName { get; }

        public ParamColumn(string columnName, int priorityInCategory = 0)
        {
            ColumnName = columnName;
            PriorityInCategory = priorityInCategory;
        }

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase) =>
            benchmarkCase.Parameters.Items.FirstOrDefault(item => item.Name == ColumnName)?.ToDisplayText(summary.Style) ??
            ParameterInstance.NullParameterTextRepresentation;

        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Params;
        public int PriorityInCategory { get; private set; }
        public override string ToString() => ColumnName;
        public bool IsNumeric => false;
        public UnitType UnitType => UnitType.Dimensionless;
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);

        public string Legend => $"Value of the '{ColumnName}' parameter";
    }
}