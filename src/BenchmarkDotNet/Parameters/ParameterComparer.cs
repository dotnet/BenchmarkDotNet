using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Parameters
{
    internal class ParameterComparer : IComparer<ParameterInstances>
    {
        public static readonly ParameterComparer Instance = new ParameterComparer();

        public int Compare(ParameterInstances x, ParameterInstances y)
        {
            if (x == null && y == null) return 0;
            if (x != null && y == null) return 1;
            if (x == null) return -1;
            for (int i = 0; i < Math.Min(x.Count, y.Count); i++)
            {
                var compareTo = CompareValues(x[i]?.Value, y[i]?.Value);
                if (compareTo != 0)
                    return compareTo;
            }
            return string.CompareOrdinal(x.DisplayInfo, y.DisplayInfo);
        }

        private int CompareValues(object x, object y)
        {
            // Detect IComparable implementations.
            // This works for all primitive types in addition to user types that implement IComparable.
            if (x != null && y != null && x.GetType() == y.GetType() &&
                x is IComparable xComparable)
            {
                try
                {
                    return xComparable.CompareTo(y);
                }
                // Some types, such as Tuple and ValueTuple, have a fallible CompareTo implementation which can throw if the inner items don't implement IComparable.
                // See: https://github.com/dotnet/BenchmarkDotNet/issues/2346
                // For now, catch and ignore the exception, and fallback to string comparison below.
                catch (ArgumentException ex) when (ex.Message.Contains("At least one object must implement IComparable."))
                {
                }
            }

            // Anything else.
            return string.CompareOrdinal(x?.ToString(), y?.ToString());
        }
    }
}