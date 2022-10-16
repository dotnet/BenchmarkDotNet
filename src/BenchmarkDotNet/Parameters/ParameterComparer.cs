using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Parameters
{
    internal class ParameterComparer : IComparer<ParameterInstances>
    {
        public static readonly ParameterComparer Instance = new ParameterComparer();

        // We will only worry about common, basic types, i.e. int, long, double, etc
        // (e.g. you can't write [Params(10.0m, 20.0m, 100.0m, 200.0m)], the compiler won't let you!)
        private static readonly Comparer PrimitiveComparer = new Comparer().
            Add((string x, string y) => string.CompareOrdinal(x, y)).
            Add((int x, int y) => x.CompareTo(y)).
            Add((long x, long y) => x.CompareTo(y)).
            Add((short x, short y) => x.CompareTo(y)).
            Add((float x, float y) => x.CompareTo(y)).
            Add((double x, double y) => x.CompareTo(y));

        public int Compare(ParameterInstances x, ParameterInstances y)
        {
            if (x == null && y == null) return 0;
            if (x != null && y == null) return 1;
            if (x == null) return -1;
            for (int i = 0; i < Math.Min(x.Count, y.Count); i++)
            {
                //todo: compare non-primitive types too
                int compareTo = PrimitiveComparer.CompareTo(x[i]?.Value, y[i]?.Value);
                if (compareTo != 0)
                    return compareTo;

                int arrayCompareTo = CompareArrays(x[i]?.Value, y[i]?.Value);
                if (arrayCompareTo != 0)
                    return arrayCompareTo;
            }
            return string.CompareOrdinal(x.DisplayInfo, y.DisplayInfo);
        }

        private static int CompareArrays(object x, object y)
        {
            if (x is Array xArray && y is Array yArray)
            {
                for (int i = 0; i < Math.Min(xArray.Length, yArray.Length); i++)
                {
                    //todo: compare non-primitive types too
                    int compareTo = PrimitiveComparer.CompareTo(xArray.GetValue(i), yArray.GetValue(i));
                    if (compareTo != 0)
                        return compareTo;
                }
                if (xArray.Length != yArray.Length)
                    return xArray.Length.CompareTo(yArray.Length);
            }
            return 0;
        }

        private class Comparer
        {
            private readonly Dictionary<Type, Func<object, object, int>> comparers =
                new Dictionary<Type, Func<object, object, int>>();

            public Comparer Add<T>(Func<T, T, int> compareFunc)
            {
                comparers.Add(typeof(T), (x, y) => compareFunc((T)x, (T)y));
                return this;
            }

            public int CompareTo(object x, object y)
            {
                return x != null && y != null && x.GetType() == y.GetType() && comparers.TryGetValue(GetComparisonType(x), out var comparer)
                    ? comparer(x, y)
                    : string.CompareOrdinal(x?.ToString(), y?.ToString());
            }

            private static Type GetComparisonType(object x) =>
                x.GetType().IsEnum
                ? x.GetType().GetEnumUnderlyingType()
                : x.GetType();
        }
    }
}