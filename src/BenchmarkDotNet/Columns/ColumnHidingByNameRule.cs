using JetBrains.Annotations;
using System;

namespace BenchmarkDotNet.Columns
{
    [PublicAPI]
    public sealed class ColumnHidingByNameRule : IColumnHidingRule, IEquatable<ColumnHidingByNameRule>
    {
        public string Name { get; }

        public ColumnHidingByNameRule(string name) => Name = name;

        public bool NeedToHide(IColumn column) => column.ColumnName == Name;

        public bool Equals(ColumnHidingByNameRule? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return Name == other.Name;
        }

        public override bool Equals(object? obj)
            => Equals(obj as ColumnHidingByNameRule);

        public override int GetHashCode()
            => HashCode.Combine(Name);
    }
}
