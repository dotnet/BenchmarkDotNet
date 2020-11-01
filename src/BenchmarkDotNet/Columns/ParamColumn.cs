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

        public ParamColumn(string columnName)
        {
            ColumnName = columnName;
        }

        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase) =>
            benchmarkCase.Parameters.Items.FirstOrDefault(item => item.Name == ColumnName)?.ToDisplayText(summary.GetCultureInfo()) ??
            ParameterInstance.NullParameterTextRepresentation;

        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Params;
        public int PriorityInCategory => 0;
        public override string ToString() => ColumnName;
        public bool IsNumeric => false;
        public UnitType UnitType => UnitType.Dimensionless;
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);

        public string Legend => $"Value of the '{ColumnName}' parameter";
    }
}