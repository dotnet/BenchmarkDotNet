using System.Collections.Generic;

namespace BenchmarkDotNet.Helpers
{
    public static class EnumerableHelper
    {
        public static IEnumerable<T> Empty<T>()
        {
            yield break;
        }
    }
}