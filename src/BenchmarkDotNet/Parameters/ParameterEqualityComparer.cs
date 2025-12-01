using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace BenchmarkDotNet.Parameters
{
    internal class ParameterEqualityComparer : IEqualityComparer<ParameterInstances>
    {
        public static readonly ParameterEqualityComparer Instance = new ParameterEqualityComparer();

        public bool Equals(ParameterInstances x, ParameterInstances y)
        {
            if (x == null && y == null) return true;
            if (x != null && y == null) return false;
            if (x == null) return false;

            for (int i = 0; i < Math.Min(x.Count, y.Count); i++)
            {
                var isEqual = ValuesEqual(x[i]?.Value, y[i]?.Value);

                if (!isEqual) return false;
            }

            return true;
        }

        private bool ValuesEqual(object x, object y)
        {
            if (x == null && y == null) return true;

            if (x != null && y != null && x.GetType() == y.GetType())
            {
                if (x is IEnumerable xEnumerable && y is IEnumerable yEnumerable) // Collection equality support
                {
                    if (x is Array xArr && y is Array yArr) // Check rank here for arrays because their values will get compared when flattened
                    {
                        if (xArr.Rank != yArr.Rank) return false;
                    }

                    var xFlat = xEnumerable.OfType<object>().ToArray();
                    var yFlat = yEnumerable.OfType<object>().ToArray();

                    if (xFlat.Length != yFlat.Length) return false;

                    return StructuralComparisons.StructuralEqualityComparer.Equals(xFlat, yFlat);
                }
                else
                {
                    // The user should define a value-based Equals check
                    return x.Equals(y);
                }
            }

            // The objects are of different types or one is null, they cannot be equal
            return false;
        }

        public int GetHashCode(ParameterInstances obj)
        {
            return obj?.ValueInfo.GetHashCode() ?? 0;
        }
    }
}