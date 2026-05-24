using System.Collections;
using System.Reflection;
using BenchmarkDotNet.Portability;

#if NETSTANDARD2_0
using System.Collections.Concurrent;
using System.Linq;
#endif

namespace BenchmarkDotNet.Parameters;

internal class DeepEqualityComparer : IEqualityComparer
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

    public static readonly DeepEqualityComparer Instance = new();

    public new bool Equals(object? x, object? y)
        => ReferenceEquals(x, y)
        || ValuesEqual(x, y);

    private static bool ValuesEqual<T1, T2>(T1 x, T2 y)
    {
        if (x == null && y == null)
        {
            return true;
        }

        if (x == null || y == null)
        {
            return false;
        }

        if (x.GetType() != y.GetType())
        {
            // Different runtime types can still be equal if either declares IEquatable<TOther> for the other,
            // or if both are collections with matching contents (e.g. List<int> vs int[], HashSet vs SortedSet).
            if (TryEquatable(x, y) || TryEquatable(y, x))
                return true;
            if (x is IDictionary xDictCross && y is IDictionary yDictCross)
                return DictionariesEqual(xDictCross, yDictCross);
            if (x is IEnumerable xEnumCross && y is IEnumerable yEnumCross)
            {
                return ImplementsSet(x) && ImplementsSet(y)
                    ? SetsEqual(xEnumCross, yEnumCross)
                    : EnumerablesEqual(xEnumCross, yEnumCross);
            }
            return false;
        }

        if (x is IDictionary xDict) // Dictionary iteration order is not stable, so compare via sorted keys.
        {
            return DictionariesEqual(xDict, (IDictionary) y);
        }

        if (ImplementsSet(x)) // Set iteration order is not stable, so compare via sorted elements.
        {
            return SetsEqual((IEnumerable) x, (IEnumerable) y);
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

        return x.Equals(y);
    }

    private static bool ImplementsSet(object obj)
        => obj.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISet<>));

    private static bool SetsEqual(IEnumerable x, IEnumerable y)
    {
        var yItems = y.Cast<object>().ToList();
        foreach (var xItem in x)
        {
            int matched = -1;
            for (int i = 0; i < yItems.Count; i++)
            {
                if (ValuesEqual(xItem, yItems[i]))
                {
                    matched = i;
                    break;
                }
            }
            if (matched < 0)
                return false;
            yItems.RemoveAt(matched);
        }
        return yItems.Count == 0;
    }

    private static bool DictionariesEqual(IDictionary x, IDictionary y)
    {
        if (x.Count != y.Count)
            return false;
        foreach (var key in x.Keys)
        {
            if (!y.Contains(key) || !ValuesEqual(x[key], y[key]))
                return false;
        }
        return true;
    }

    private static bool TryEquatable(object x, object y)
    {
        var yType = y.GetType();
        var equatableInterface = x.GetType().GetInterfaces().FirstOrDefault(i => i.IsGenericType
            && i.GetGenericTypeDefinition() == typeof(IEquatable<>)
            && i.GetGenericArguments()[0].IsAssignableFrom(yType));

        if (equatableInterface == null)
            return false;

        var method = equatableInterface.GetMethod(nameof(IEquatable<object>.Equals), BindingFlags.Public | BindingFlags.Instance);
        return (bool?) method?.Invoke(x, [y]) ?? false;
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
                equals = (bool) typeof(DeepEqualityComparer)
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
                for (int k = 0; k < arrOne.GetLength(2); k++)
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

    public int GetHashCode(object? obj)
        => obj?.GetHashCode() ?? 0;
}