using System.Collections.Generic;

namespace BenchmarkDotNet.Running
{
    internal class TypeComparer : IComparer<Descriptor>
    {
        public static readonly IComparer<Descriptor> Instance = new TypeComparer();

        public int Compare(Descriptor x, Descriptor y)
        {
            if (x == null && y == null) return 0;
            if (x != null && y == null) return 1;
            if (x == null) return -1;
            return string.CompareOrdinal(x.TypeInfo, y.TypeInfo);
        }
    }
}