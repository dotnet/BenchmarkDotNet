using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BenchmarkDotNet.Portability;

#if NETSTANDARD2_0
using System.Collections.Concurrent;
using System.Linq;
#endif

#nullable enable

namespace BenchmarkDotNet.Parameters
{
    internal class ParameterEqualityComparer : IEqualityComparer<ParameterInstances>
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

        public static readonly ParameterEqualityComparer Instance = new ParameterEqualityComparer();

        public bool Equals(ParameterInstances? x, ParameterInstances? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (x is null || y is null)
                return false;

            if (x.Count != y.Count) return false;

            for (int i = 0; i < x.Count; i++)
            {
                if (!ValuesEqual(x[i]?.Value, y[i]?.Value))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValuesEqual<T1, T2>(T1 x, T2 y)
        {
            if (x == null && y == null)
            {
                return true;
            }

            if (x == null || y == null || x.GetType() != y.GetType())
            {
                // The objects are of different types or one is null, they cannot be equal
                return false;
            }

            if (x is IStructuralEquatable xStructuralEquatable)
            {
                try
                {
                    return StructuralComparisons.StructuralEqualityComparer.Equals(xStructuralEquatable, y);
                }
                // https://github.com/dotnet/runtime/issues/66472
                // Unfortunately we can't rely on checking the exception message because it may change per current culture.
                catch (ArgumentException)
                {
                    if (TryFallbackStructuralEquals(x, y, out bool equals))
                    {
                        return equals;
                    }
                    // A complex user type did not handle a multi-dimensional array, just re-throw.
                    throw;
                }
            }

            if (x is IEnumerable xEnumerable) // General collection equality support
            {
                return EnumerablesEqual(xEnumerable, (IEnumerable) y);
            }

            return FallbackSimpleEquals(x, y);
        }

        private static bool FallbackSimpleEquals(object x, object y)
        {
            if (x.Equals(y))
            {
                return true;
            }
            // Anything else to differentiate between objects (match behavior of ParameterComparer).
            return string.Equals(x.ToString(), y.ToString(), StringComparison.Ordinal);
        }

        private static bool TryFallbackStructuralEquals(object x, object y, out bool equals)
        {
            // Check for multi-dimensional array and ITuple and re-try for each element recursively.
            if (x is Array xArr)
            {
                Array yArr = (Array) y;
                if (xArr.Rank != yArr.Rank)
                {
                    equals = false;
                    return true;
                }

                for (int dim = 0; dim < xArr.Rank; dim++)
                {
                    if (xArr.GetLength(dim) != yArr.GetLength(dim))
                    {
                        equals = false;
                        return true;
                    }
                }

                // Common 2D and 3D arrays are specialized to avoid expensive boxing where possible.
                if (!RuntimeInformation.IsAot && xArr.Rank is 2 or 3)
                {
                    string methodName = xArr.Rank == 2
                        ? nameof(TwoDArraysEqual)
                        : nameof(ThreeDArraysEqual);
                    equals = (bool) typeof(ParameterEqualityComparer)
                        .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!
                        .MakeGenericMethod(xArr.GetType().GetElementType()!, yArr.GetType().GetElementType()!)
                        .Invoke(null, [xArr, yArr])!;
                    return true;
                }

                // 1D arrays will only hit this code path if a nested type is a multi-dimensional array.
                // 4D and larger fall back to enumerable.
                equals = EnumerablesEqual(xArr, yArr);
                return true;
            }

#if NETSTANDARD2_0
            // ITuple does not exist in netstandard2.0, so we have to use reflection. ITuple does exist in net471 and newer, but the System.ValueTuple nuget package does not implement it.
            string typeName = x.GetType().FullName;
            if (typeName.StartsWith("System.Tuple`"))
            {
                equals = TuplesEqual(x, y);
                return true;
            }
            else if (typeName.StartsWith("System.ValueTuple`"))
            {
                equals = ValueTuplesEqual(x, y);
                return true;
            }
#else
            if (x is System.Runtime.CompilerServices.ITuple xTuple)
            {
                equals = TuplesEqual(xTuple, (System.Runtime.CompilerServices.ITuple) y);
                return true;
            }
#endif

            if (x is IEnumerable xEnumerable) // General collection equality support
            {
                equals = EnumerablesEqual(xEnumerable, (IEnumerable) y);
                return true;
            }

            equals = false;
            return false;
        }

        private static bool EnumerablesEqual(IEnumerable x, IEnumerable y)
        {
            var xEnumerator = x.GetEnumerator();
            try
            {
                var yEnumerator = y.GetEnumerator();
                try
                {
                    while (xEnumerator.MoveNext())
                    {
                        if (!(yEnumerator.MoveNext() && ValuesEqual(xEnumerator.Current, yEnumerator.Current)))
                        {
                            return false;
                        }
                    }
                    return !yEnumerator.MoveNext();
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

        private static bool TwoDArraysEqual<T1, T2>(T1[,] arrOne, T2[,] arrTwo)
        {
            // Assumes that arrOne & arrTwo are the same length and width.
            for (int i = 0; i < arrOne.GetLength(0); i++)
            {
                for (int j = 0; j < arrOne.GetLength(1); j++)
                {
                    if (!ValuesEqual(arrOne[i, j], arrTwo[i, j]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool ThreeDArraysEqual<T1, T2>(T1[,,] arrOne, T2[,,] arrTwo)
        {
            // Assumes that arrOne & arrTwo are the same length, width, and height.
            for (int i = 0; i < arrOne.GetLength(0); i++)
            {
                for (int j = 0; j < arrOne.GetLength(1); j++)
                {
                    for (int k = 0; k <arrOne.GetLength(2); k++)
                    {
                        if (!ValuesEqual(arrOne[i, j, k], arrTwo[i, j, k]))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

#if NETSTANDARD2_0
        private static bool TuplesEqual(object x, object y)
        {
            foreach (PropertyInfo property in GetTupleMembers(x.GetType()))
            {
                if (!ValuesEqual(property.GetValue(x), property.GetValue(y)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValueTuplesEqual(object x, object y)
        {
            foreach (FieldInfo field in GetTupleMembers(x.GetType()))
            {
                if (!ValuesEqual(field.GetValue(x), field.GetValue(y)))
                {
                    return false;
                }
            }

            return true;
        }
#else
        private static bool TuplesEqual(System.Runtime.CompilerServices.ITuple x, System.Runtime.CompilerServices.ITuple y)
        {
            for (int i = 0; i < x.Length; i++)
            {
                if (!ValuesEqual(x[i], y[i]))
                {
                    return false;
                }
            }

            return true;
        }
#endif

        public int GetHashCode(ParameterInstances obj)
        {
            return obj?.ValueInfo.GetHashCode() ?? 0;
        }
    }
}