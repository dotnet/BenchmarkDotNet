using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

        private bool ValuesEqual<T1, T2>(T1 x, T2 y)
        {
            if (x == null && y == null) return true;

            if (x != null && y != null && x.GetType() == y.GetType())
            {
                if (x is IStructuralEquatable)
                {
                    if (x is Array xArr && y is Array yArr)
                    {
                        if (xArr.Rank != yArr.Rank) return false;

                        for (int dim = 0; dim < xArr.Rank; dim++)
                        {
                            if (xArr.GetLength(dim) != yArr.GetLength(dim)) return false;
                        }

                        if (xArr.Rank == 1) return StructuralEqualityWithFallback(xArr, yArr);

                        if (xArr.Rank == 2)
                        {
                            return (bool) GetType()
                                .GetMethod(nameof(TwoDimensionalArrayEquals), BindingFlags.NonPublic | BindingFlags.Instance)
                                .MakeGenericMethod(xArr.GetType().GetElementType(), yArr.GetType().GetElementType())
                                .Invoke(this, [xArr, yArr]);
                        }

                        if (xArr.Rank == 3)
                        {
                            return (bool) GetType()
                                .GetMethod(nameof(ThreeDimensionalArrayEquals), BindingFlags.NonPublic | BindingFlags.Instance)
                                .MakeGenericMethod(xArr.GetType().GetElementType(), yArr.GetType().GetElementType())
                                .Invoke(this, [xArr, yArr]);
                        }

                        return EnumerablesEqual(xArr, yArr);
                    }
                    else // Probably a user-defined IStructuralComparable, as tuples would be handled by the IComparable case
                    {
                        return StructuralComparisons.StructuralEqualityComparer.Equals(x, y);
                    }
                }
                else if (x is IEnumerable xEnumerable  && y is IEnumerable yEnumerable) // General collection equality support
                {
                    return EnumerablesEqual(xEnumerable, yEnumerable);
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

        private bool StructuralEqualityWithFallback(Array x, Array y)
        {
            try
            {
                return StructuralComparisons.StructuralEqualityComparer.Equals(x, y);
            }
            // Inner element type does not support comparison, hash elements to compare collections
            catch (ArgumentException ex) when (ex.Message.Contains("At least one object must implement IComparable."))
            {
                return EnumerablesEqual(x.OfType<object>().Select(elem => elem.GetHashCode()), y.OfType<object>().Select(elem => elem.GetHashCode()));
            }
        }

        private bool EnumerablesEqual(IEnumerable nonGenericX, IEnumerable nonGenericY)
        {
            // Use this instead of StructuralComparisons.StructuralEqualityComparer to avoid resolving the whole enumerable to object[]

            var x = nonGenericX.OfType<object>();
            var y = nonGenericY.OfType<object>();

            foreach (var (xElem, yElem) in x.Zip(y, (x, y) => (x, y)))
            {
                bool res = ValuesEqual(xElem, yElem);

                if (!res) return false;
            }

            return true;
        }

        public int GetHashCode(ParameterInstances obj)
        {
            return obj?.ValueInfo.GetHashCode() ?? 0;
        }

        private bool TwoDimensionalArrayEquals<T1, T2>(T1[,] arrOne, T2[,] arrTwo)
        {
            // Assume that arrOne & arrTwo are the same Length & Rank

            for (int i = 0; i < arrOne.GetLength(0); i++)
            {
                for (int j = 0; j < arrOne.GetLength(1); j++)
                {
                    var x = arrOne[i, j];
                    var y = arrTwo[i, j];

                    var res = ValuesEqual(x, y);

                    if (!res) return false;
                }
            }

            return true;
        }

        private bool ThreeDimensionalArrayEquals<T1, T2>(T1[,,] arrOne, T2[,,] arrTwo)
        {
            // Assume that arrOne & arrTwo are the same Length & Rank

            for (int i = 0; i < arrOne.GetLength(0); i++)
            {
                for (int j = 0; j < arrOne.GetLength(1); j++)
                {
                    for (int k = 0; k <arrOne.GetLength(2); k++)
                    {
                        var x = arrOne[i, j, k];
                        var y = arrTwo[i, j, k];

                        var res = ValuesEqual(x, y);

                        if (!res) return false;
                    }
                }
            }

            return true;
        }
    }
}