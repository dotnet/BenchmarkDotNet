using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BenchmarkDotNet.Portability;

#if NETSTANDARD2_0
using System.Collections.Concurrent;
using System.Linq;
#endif

namespace BenchmarkDotNet.Parameters
{
    internal class ParameterComparer : IComparer<ParameterInstances>
    {
#if NETSTANDARD2_0
        private static readonly ConcurrentDictionary<Type, MemberInfo[]> s_tupleMembersCache = new();

        private static MemberInfo[] GetTupleMembers(Type type)
            => s_tupleMembersCache.GetOrAdd(type, t =>
            {
                var members = type.FullName.StartsWith("System.Tuple`")
                    ? type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Cast<MemberInfo>()
                    : type.GetFields(BindingFlags.Public | BindingFlags.Instance).Cast<MemberInfo>();
                return members
                    .Where(p => p.Name.StartsWith("Item"))
                    .OrderBy(p => p.Name)
                    .ToArray();
            });
#endif

        public static readonly ParameterComparer Instance = new ParameterComparer();

        public int Compare(ParameterInstances x, ParameterInstances y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            for (int i = 0; i < Math.Min(x.Count, y.Count); i++)
            {
                var comparison = CompareValues(x[i]?.Value, y[i]?.Value);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return string.CompareOrdinal(x.DisplayInfo, y.DisplayInfo);
        }

        private static int CompareValues<T1, T2>(T1 x, T2 y)
        {
            if (x == null || y == null || x.GetType() != y.GetType())
            {
                return string.CompareOrdinal(x?.ToString(), y?.ToString());
            }

            if (x is IStructuralComparable xStructuralComparable)
            {
                try
                {
                    return StructuralComparisons.StructuralComparer.Compare(x, y);
                }
                // https://github.com/dotnet/BenchmarkDotNet/issues/2346
                // https://github.com/dotnet/runtime/issues/66472
                // Unfortunately we can't rely on checking the exception message because it may change per current culture.
                catch (ArgumentException)
                {
                    if (TryFallbackStructuralCompareTo(x, y, out int comparison))
                    {
                        return comparison;
                    }
                    // A complex user type did not handle a multi-dimensional array or tuple, just re-throw.
                    throw;
                }
            }

            if (x is IComparable xComparable)
            {
                // Tuples are already handled by IStructuralComparable case, if this throws, it's the user's own fault.
                return xComparable.CompareTo(y);
            }

            if (x is IEnumerable xEnumerable) // General collection comparison support
            {
                return CompareEnumerables(xEnumerable, (IEnumerable) y);
            }

            // Anything else to differentiate between objects.
            return string.CompareOrdinal(x?.ToString(), y?.ToString());
        }

        private static bool TryFallbackStructuralCompareTo(object x, object y, out int comparison)
        {
            // Check for multi-dimensional array and ITuple and re-try for each element recursively.
            if (x is Array xArr)
            {
                Array yArr = (Array) y;
                if (xArr.Rank != yArr.Rank)
                {
                    comparison = xArr.Rank.CompareTo(yArr.Rank);
                    return true;
                }

                for (int dim = 0; dim < xArr.Rank; dim++)
                {
                    if (xArr.GetLength(dim) != yArr.GetLength(dim))
                    {
                        comparison = xArr.GetLength(dim).CompareTo(yArr.GetLength(dim));
                        return true;
                    }
                }

                // Common 2D and 3D arrays are specialized to avoid expensive boxing where possible.
                if (!RuntimeInformation.IsAot && xArr.Rank is 2 or 3)
                {
                    string methodName = xArr.Rank == 2
                        ? nameof(CompareTwoDArray)
                        : nameof(CompareThreeDArray);
                    comparison = (int) typeof(ParameterComparer)
                        .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
                        .MakeGenericMethod(xArr.GetType().GetElementType(), yArr.GetType().GetElementType())
                        .Invoke(null, [xArr, yArr]);
                    return true;
                }

                // 1D arrays will only hit this code path if a nested type is a multi-dimensional array.
                // 4D and larger fall back to enumerable.
                comparison = CompareEnumerables(xArr, yArr);
                return true;
            }

#if NETSTANDARD2_0
            // ITuple does not exist in netstandard2.0, so we have to use reflection. ITuple does exist in net471 and newer, but the System.ValueTuple nuget package does not implement it.
            string typeName = x.GetType().FullName;
            if (typeName.StartsWith("System.Tuple`"))
            {
                comparison = CompareTuples(x, y);
                return true;
            }
            else if (typeName.StartsWith("System.ValueTuple`"))
            {
                comparison = CompareValueTuples(x, y);
                return true;
            }
#else
            if (x is System.Runtime.CompilerServices.ITuple xTuple)
            {
                comparison = CompareTuples(xTuple, (System.Runtime.CompilerServices.ITuple) y);
                return true;
            }
#endif

            if (x is IEnumerable xEnumerable) // General collection equality support
            {
                comparison = CompareEnumerables(xEnumerable, (IEnumerable) y);
                return true;
            }

            comparison = 0;
            return false;
        }

        private static int CompareEnumerables(IEnumerable x, IEnumerable y)
        {
            var xEnumerator = x.GetEnumerator();
            try
            {
                var yEnumerator = y.GetEnumerator();
                try
                {
                    while (xEnumerator.MoveNext())
                    {
                        if (!yEnumerator.MoveNext())
                        {
                            return -1;
                        }
                        int comparison = CompareValues(xEnumerator.Current, yEnumerator.Current);
                        if (comparison != 0)
                        {
                            return comparison;
                        }
                    }
                    return yEnumerator.MoveNext() ? 1 : 0;
                }
                finally
                {
                    if (yEnumerator is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
            finally
            {
                if (xEnumerator is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        private static int CompareTwoDArray<T1, T2>(T1[,] arrOne, T2[,] arrTwo)
        {
            // Assumes that arrOne & arrTwo are the same length and width.
            for (int i = 0; i < arrOne.GetLength(0); i++)
            {
                for (int j = 0; j < arrOne.GetLength(1); j++)
                {
                    var comparison = CompareValues(arrOne[i, j], arrTwo[i, j]);
                    if (comparison != 0)
                    {
                        return comparison;
                    }
                }
            }

            return 0;
        }

        private static int CompareThreeDArray<T1, T2>(T1[,,] arrOne, T2[,,] arrTwo)
        {
            // Assumes that arrOne & arrTwo are the same length, width, and height.
            for (int i = 0; i < arrOne.GetLength(0); i++)
            {
                for (int j = 0; j < arrOne.GetLength(1); j++)
                {
                    for (int k = 0; k <arrOne.GetLength(2); k++)
                    {
                        var comparison = CompareValues(arrOne[i, j, k], arrTwo[i, j, k]);
                        if (comparison != 0)
                        {
                            return comparison;
                        }
                    }
                }
            }

            return 0;
        }

#if NETSTANDARD2_0
        private static int CompareTuples(object x, object y)
        {
            foreach (PropertyInfo property in GetTupleMembers(x.GetType()))
            {
                var comparison = CompareValues(property.GetValue(x), property.GetValue(y));
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return 0;
        }

        private static int CompareValueTuples(object x, object y)
        {
            foreach (FieldInfo field in GetTupleMembers(x.GetType()))
            {
                var comparison = CompareValues(field.GetValue(x), field.GetValue(y));
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return 0;
        }
#else
        private static int CompareTuples(System.Runtime.CompilerServices.ITuple x, System.Runtime.CompilerServices.ITuple y)
        {
            for (int i = 0; i < x.Length; i++)
            {
                var comparison = CompareValues(x[i], y[i]);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return 0;
        }
#endif
    }
}