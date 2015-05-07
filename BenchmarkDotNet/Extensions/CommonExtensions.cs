using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BenchmarkDotNet.Extensions
{
    internal static class CommonExtensions
    {
        public static double Median(this IEnumerable<double> source)
        {
            var list = source.ToList();
            if (list.Count == 0)
                throw new InvalidOperationException("Sequence contains no elements");
            list.Sort();
            if (list.Count % 2 == 1)
                return list[list.Count / 2];
            return (list[list.Count / 2 - 1] + list[list.Count / 2]) / 2;
        }

        public static double StandardDeviation(this IEnumerable<double> source)
        {
            var list = source.ToList();
            double result = 0;
            if (list.Any())
            {
                double avg = list.Average();
                double sum = list.Sum(d => Math.Pow(d - avg, 2));
                result = Math.Sqrt(sum / list.Count());
            }
            return result;
        }

        internal static string WithoutSuffix(this string str, string suffix, StringComparison stringComparison = StringComparison.CurrentCulture)
        {
            return str.EndsWith(suffix, stringComparison) ? str.Substring(0, str.Length - suffix.Length) : str;
        }

        public static IEnumerable<T> TakeLast<T>(this IList<T> source, int count)
        {
            return source.Skip(Math.Max(0, source.Count() - count));
        }

        public static int GetStrLength(this long value)
        {
            return value.ToInvariantString().Length;
        }

        public static string ToInvariantString(this long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public static int GetStrLength(this int value)
        {
            return value.ToInvariantString().Length;
        }

        public static string ToInvariantString(this int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}