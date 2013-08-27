using System;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet
{
    public static class Extensions
    {
        public static long Median<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            var list = source.Select(selector).ToList();
            if (list.Count == 0)
                throw new InvalidOperationException("Sequence contains no elements");
            list.Sort();
            if (list.Count % 2 == 1)
                return list[list.Count / 2];
            return (list[list.Count / 2 - 1] + list[list.Count / 2]) / 2;
        }
    }
}