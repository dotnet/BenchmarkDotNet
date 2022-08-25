using JetBrains.Annotations;

namespace BenchmarkDotNet.Columns
{
    [PublicAPI]
    public class ColumnHidingByNameRule: IColumnHidingRule
    {
        public string Name { get; }

        public ColumnHidingByNameRule(string name) => Name = name;

        public bool NeedToHide(IColumn column) => column.ColumnName == Name;
    }
}