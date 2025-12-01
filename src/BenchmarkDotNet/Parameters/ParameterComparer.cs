using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
            if (x != null && y != null && x.GetType() == y.GetType())
            {
                if (x is IComparable xComparable)
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
                else if (x is IEnumerable xEnumerable  && y is IEnumerable yEnumerable) // collection equality support
                {
                    if (x is Array xArr && y is Array yArr) // check rank here for arrays because their values will get compared when flattened
                    {
                        if (xArr.Rank != yArr.Rank)
                            return xArr.Rank.CompareTo(yArr.Rank);
                    }

                    var xFlat = xEnumerable.OfType<object>().ToArray();
                    var yFlat = yEnumerable.OfType<object>().ToArray();

                    if (xFlat.Length != yFlat.Length)
                        return xFlat.Length.CompareTo(yFlat.Length);

                    try
                    {
                        return StructuralComparisons.StructuralComparer.Compare(xFlat, yFlat);
                    }
                    // Inner element type does not support comparison, hash elements to compare collections
                    catch (ArgumentException ex) when (ex.Message.Contains("At least one object must implement IComparable."))
                    {
                        var xFlatHashed = xFlat.Select(elem => elem.GetHashCode()).ToArray();
                        var yFlatHashed = yFlat.Select(elem => elem.GetHashCode()).ToArray();
                        return StructuralComparisons.StructuralComparer.Compare(xFlatHashed, yFlatHashed);
                    }
                }
            }

            // Anything else.
            return string.CompareOrdinal(x?.ToString(), y?.ToString());
        }
    }
}