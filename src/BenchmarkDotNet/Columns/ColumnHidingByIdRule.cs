using JetBrains.Annotations;
using System;

namespace BenchmarkDotNet.Columns
{
    [PublicAPI]
    public sealed class ColumnHidingByIdRule : IColumnHidingRule, IEquatable<ColumnHidingByIdRule>
    {
        public string Id { get; }

        public ColumnHidingByIdRule(IColumn column) => Id = column.Id;

        public bool NeedToHide(IColumn column) => column.Id == Id;

        public bool Equals(ColumnHidingByIdRule? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Id == other.Id;
        }

        public override bool Equals(object? obj)
            => Equals(obj as ColumnHidingByIdRule);

        public override int GetHashCode()
            => HashCode.Combine(Id);
    }
}
