using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Portability;

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
                if (x is IStructuralEquatable xStructuralEquatable)
                {
                    if (x is Array xArr && y is Array yArr)
                    {
                        if (xArr.Rank != yArr.Rank) return false;

                        for (int dim = 0; dim < xArr.Rank; dim++)
                        {
                            if (xArr.GetLength(dim) != yArr.GetLength(dim)) return false;
                        }

                        //  1D, 2D, and 3D array comparison is optimized with dedicated methods
                        if (xArr.Rank == 1) return StructuralEquals(xArr, yArr);

                        if (xArr.Rank == 2 && !RuntimeInformation.IsAot)
                        {
                            return (bool) GetType()
                                .GetMethod(nameof(TwoDimensionalArrayEquals), BindingFlags.NonPublic | BindingFlags.Instance)
                                .MakeGenericMethod(xArr.GetType().GetElementType(), yArr.GetType().GetElementType())
                                .Invoke(this, [xArr, yArr]);
                        }

                        if (xArr.Rank == 3 && !RuntimeInformation.IsAot)
                        {
                            return (bool) GetType()
                                .GetMethod(nameof(ThreeDimensionalArrayEquals), BindingFlags.NonPublic | BindingFlags.Instance)
                                .MakeGenericMethod(xArr.GetType().GetElementType(), yArr.GetType().GetElementType())
                                .Invoke(this, [xArr, yArr]);
                        }

                        return EnumerablesEqual(xArr, yArr);
                    }
                    else // Probably a user-defined IStructuralEquatable or tuple
                    {
                        return StructuralEquals(xStructuralEquatable, (IStructuralEquatable) y);
                    }
                }
                else if (x is IEnumerable xEnumerable && y is IEnumerable yEnumerable) // General collection equality support
                {
                    return EnumerablesEqual(xEnumerable, yEnumerable);
                }
                else
                {
                    return x.Equals(y);
                }
            }

            // The objects are of different types or one is null, they cannot be equal
            return false;
        }

        private bool StructuralEquals(IStructuralEquatable x, IStructuralEquatable y)
        {
           return StructuralComparisons.StructuralEqualityComparer.Equals(x, y);
        }

        private bool EnumerablesEqual(IEnumerable x, IEnumerable y)
        {
            // Use this instead of StructuralComparisons.StructuralComparer to avoid resolving the whole enumerable to object[]

            var xEnumer = x.GetEnumerator();
            var yEnumer = y.GetEnumerator();

            bool xHasElement, yHasElement;

            // Use bitwise AND to avoid short-circuiting, which destroys this function's length checking logic
            while ((xHasElement = xEnumer.MoveNext()) & (yHasElement = yEnumer.MoveNext()))
            {
                bool res = ValuesEqual(xEnumer.Current, yEnumer.Current);

                if (!res) return false;
            }

            if (xHasElement || yHasElement) return false;

            return true;
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

                    bool res = ValuesEqual(x, y);

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

                        bool res = ValuesEqual(x, y);

                        if (!res) return false;
                    }
                }
            }

            return true;
        }

        public int GetHashCode(ParameterInstances obj)
        {
            return obj?.ValueInfo.GetHashCode() ?? 0;
        }
    }
}