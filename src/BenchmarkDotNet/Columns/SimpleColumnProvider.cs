using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Columns
{
    public class SimpleColumnProvider : IColumnProvider, IEquatable<SimpleColumnProvider>
    {
        private readonly IColumn[] columns;

        public SimpleColumnProvider(params IColumn[] columns)
        {
            this.columns = columns;
        }

        public IEnumerable<IColumn> GetColumns(Summary summary) => columns.Where(c => c.IsAvailable(summary));

        public bool Equals(SimpleColumnProvider? other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (other is null)
                return false;

            return columns.SequenceEqual(other.columns);
        }

        public override bool Equals(object? obj)
           => Equals(obj as SimpleColumnProvider);

        public override int GetHashCode()
        {
            // Compute hashcode of each column.
            var hash = new HashCode();
            foreach (var column in columns)
                hash.Add(column);
            return hash.ToHashCode();
        }
    }
}
