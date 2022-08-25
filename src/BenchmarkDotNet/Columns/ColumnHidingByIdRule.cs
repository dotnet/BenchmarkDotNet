using JetBrains.Annotations;

namespace BenchmarkDotNet.Columns
{
    [PublicAPI]
    public class ColumnHidingByIdRule: IColumnHidingRule
    {
        public string Id { get; }

        public ColumnHidingByIdRule(IColumn column) => Id = column.Id;

        public bool NeedToHide(IColumn column) => column.Id == Id;
    }
}