using System;
using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet
{
    public static class BenchmarkExtensions
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

        public static double StandardDeviation<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            var list = source.Select(selector).ToList();
            double result = 0;
            if (list.Any())
            {
                double avg = list.Average();
                double sum = list.Sum(d => Math.Pow(d - avg, 2));
                result = Math.Sqrt(sum / list.Count());
            }
            return result;
        }

        public static string ToCultureString(this long value)
        {
            return value.ToString(BenchmarkSettings.Instance.CultureInfo);
        }

        internal static string WithoutSuffix(this string str, string suffix, StringComparison stringComparison = StringComparison.CurrentCulture)
        {
            return str.EndsWith(suffix, stringComparison) ? str.Substring(0, str.Length - suffix.Length) : str;
        }
    }
}