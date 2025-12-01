using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

        private int CompareValues<T1, T2>(T1 x, T2 y)
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
                else if (x is IStructuralComparable)
                {
                    if (x is Array xArr && y is Array yArr)
                    {
                        if (xArr.Rank != yArr.Rank) return xArr.Rank.CompareTo(yArr.Rank);

                        for (int dim = 0; dim < xArr.Rank; dim++)
                        {
                            if (xArr.GetLength(dim) != yArr.GetLength(dim))
                                return xArr.GetLength(dim).CompareTo(yArr.GetLength(dim));
                        }

                        //  1D, 2D, and 3D array comparison is optimized with dedicated methods
                        if (xArr.Rank == 1) return StructuralComparisonWithFallback(xArr, yArr);

                        if (xArr.Rank == 2)
                        {
                            return (int) GetType()
                                .GetMethod(nameof(CompareTwoDimensionalArray), BindingFlags.NonPublic | BindingFlags.Instance)
                                .MakeGenericMethod(xArr.GetType().GetElementType(), yArr.GetType().GetElementType())
                                .Invoke(this, [xArr, yArr]);
                        }

                        if (xArr.Rank == 3)
                        {
                            return (int) GetType()
                                .GetMethod(nameof(CompareThreeDimensionalArray), BindingFlags.NonPublic | BindingFlags.Instance)
                                .MakeGenericMethod(xArr.GetType().GetElementType(), yArr.GetType().GetElementType())
                                .Invoke(this, [xArr, yArr]);
                        }

                        return CompareEnumerables(xArr, yArr);
                    }
                    else // Probably a user-defined IStructuralComparable, as tuples would be handled by the IComparable case
                    {
                        return StructuralComparisons.StructuralComparer.Compare(x, y);
                    }
                }
                else if (x is IEnumerable xEnumerable  && y is IEnumerable yEnumerable) // General collection equality support
                {
                    return CompareEnumerables(xEnumerable, yEnumerable);
                }
            }

            // Anything else to differentiate between objects.
            var stringComp = string.CompareOrdinal(x?.ToString(), y?.ToString());

            if (stringComp != 0) return stringComp;

            return x?.GetHashCode().CompareTo(y?.GetHashCode() ?? 0) ?? 0;
        }

        private int StructuralComparisonWithFallback(Array x, Array y)
        {
            try
            {
                return StructuralComparisons.StructuralComparer.Compare(x, y);
            }
            // Inner element type does not support comparison, hash elements to compare collections
            catch (ArgumentException ex) when (ex.Message.Contains("At least one object must implement IComparable."))
            {
                var xFlatHashed = x.OfType<object>().Select(elem => elem.GetHashCode());
                var yFlatHashed = y.OfType<object>().Select(elem => elem.GetHashCode());
                return CompareEnumerables(xFlatHashed, yFlatHashed);
            }
        }

        private int CompareEnumerables(IEnumerable nonGenericX, IEnumerable nonGenericY)
        {
            // Use this instead of StructuralComparisons.StructuralComparer to avoid resolving the whole enumerable to object[]

            var x = nonGenericX.OfType<object>();
            var y = nonGenericY.OfType<object>();

            foreach (var (xElem, yElem) in x.Zip(y, (x, y) => (x, y)))
            {
                int res = CompareValues(xElem, yElem);

                if (res != 0) return res;
            }

            return 0;
        }

        private int CompareTwoDimensionalArray<T1, T2>(T1[,] arrOne, T2[,] arrTwo)
        {
            // Assume that arrOne & arrTwo are the same Length & Rank

            for (int i = 0; i < arrOne.GetLength(0); i++)
            {
                for (int j = 0; j < arrOne.GetLength(1); j++)
                {
                    var x = arrOne[i, j];
                    var y = arrTwo[i, j];

                    var res = CompareValues(x, y);

                    if (res != 0) return res;
                }
            }

            return 0;
        }

        private int CompareThreeDimensionalArray<T1, T2>(T1[,,] arrOne, T2[,,] arrTwo)
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

                        var res = CompareValues(x, y);

                        if (res != 0) return res;
                    }
                }
            }

            return 0;
        }
    }
}