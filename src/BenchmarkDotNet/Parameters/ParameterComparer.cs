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
            for (int i = 0; i < Math.Min(x.Count, y.Count); i++)
            {
                int compareTo = PrimitiveComparer.CompareTo(x[i]?.Value, y[i]?.Value);
                if (compareTo != 0)
                    return compareTo;
            }
            return string.CompareOrdinal(x.DisplayInfo, y.DisplayInfo);
        }

        private class Comparer
        {
            private readonly Dictionary<Type, Func<object, object, int>> comparers =
                new Dictionary<Type, Func<object, object, int>>();

            public Comparer Add<T>(Func<T, T, int> compareFunc)
            {
                comparers.Add(typeof(T), (x, y) => compareFunc((T) x, (T) y));
                return this;
            }

            public int CompareTo(object x, object y)
            {
                return x != null && y != null && x.GetType() == y.GetType() && HasComparer(x)
                    ? comparers[x.GetType()](x, y)
                    : string.CompareOrdinal(x?.ToString(), y?.ToString());
            }

            private bool HasComparer(object x) => comparers.ContainsKey(x.GetType());
        }
    }
}