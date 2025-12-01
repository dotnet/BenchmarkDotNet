using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace BenchmarkDotNet.Parameters
{
    internal class ParameterEqualityComparer : IEqualityComparer<ParameterInstances>
    {
        public static readonly ParameterEqualityComparer Instance = new ParameterEqualityComparer();

        public bool Equals(ParameterInstances? x, ParameterInstances? y)
        {
            return ParameterComparer.Instance.Compare(x, y) == 0;
        }

        public int GetHashCode([DisallowNull] ParameterInstances obj)
        {
            return obj.ValueInfo.GetHashCode();
        }
    }
}