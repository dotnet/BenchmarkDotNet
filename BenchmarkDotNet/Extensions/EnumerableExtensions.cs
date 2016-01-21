using System;
using System.Collections.Generic;

namespace BenchmarkDotNet.Extensions
{
    internal static class EnumerableExtensions
    {
        internal static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> command)
        {
            foreach (var item in enumerable)
            {
                command(item);
            }
        }
    }
}