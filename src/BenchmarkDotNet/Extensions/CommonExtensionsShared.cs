using System.Collections.Generic;

namespace BenchmarkDotNet.Extensions
{
    internal static partial class CommonExtensions
    {
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_0_OR_GREATER
        public static TValue? GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            => dictionary.TryGetValue(key, out var value) ? value : default;
#endif
    }
}