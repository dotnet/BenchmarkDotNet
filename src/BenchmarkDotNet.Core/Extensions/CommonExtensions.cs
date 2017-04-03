using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Horology;

namespace BenchmarkDotNet.Extensions
{
    internal static class CommonExtensions
    {
        const int BytesInKiloByte = 1000; // 1000 vs 1024 thing..

        static readonly string[] SizeSuffixes = { "B", "kB", "MB", "GB", "TB" };

        public static List<T> ToSortedList<T>(this IEnumerable<T> values)
        {
            var list = new List<T>();
            list.AddRange(values);
            list.Sort();
            return list;
        }

        public static string ToTimeStr(this double value, TimeUnit unit = null, int unitNameWidth = 1)
        {
            unit = unit ?? TimeUnit.GetBestTimeUnit(value);
            var unitValue = TimeUnit.Convert(value, TimeUnit.Nanosecond, unit);
            var unitName = unit.Name.PadLeft(unitNameWidth);
            return $"{unitValue.ToStr("N4")} {unitName}";
        }

        internal static string ToFormattedBytes(this long bytes)
        {
            int i;
            double dblSByte = bytes;
            for (i = 0; i < SizeSuffixes.Length && bytes >= BytesInKiloByte; i++, bytes /= BytesInKiloByte)
            {
                dblSByte = bytes / (double)BytesInKiloByte;
            }

            return string.Format(HostEnvironmentInfo.MainCultureInfo, "{0:0.##} {1}", dblSByte, SizeSuffixes[i]);
        }

        public static string ToStr(this double value, string format = "0.##")
        {
            // Here we should manually create an object[] for string.Format
            // If we write something like
            //     string.Format(HostEnvironmentInfo.MainCultureInfo, $"{{0:{format}}}", value)
            // it will be resolved to:
            //     string.Format(System.IFormatProvider, string, params object[]) // .NET 4.5
            //     string.Format(System.IFormatProvider, string, object)          // .NET 4.6
            // Unfortunately, Mono doesn't have the second overload (with object instead of params object[]).            
            var args = new object[] { value };
            return string.Format(HostEnvironmentInfo.MainCultureInfo, $"{{0:{format}}}", args);
        }

        public static bool IsNullOrEmpty<T>(this IReadOnlyCollection<T> value) => value == null || value.Count == 0;
        public static bool IsEmpty<T>(this IReadOnlyCollection<T> value) => value.Count == 0;

        public static T Penult<T>(this IList<T> list) => list[list.Count - 2];

        public static bool IsOneOf<T>(this T value, params T[] values) => values.Contains(value);

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            => dictionary.TryGetValue(key, out TValue value) ? value : default(TValue);

        public static double Sqr(this double x) => x * x;
        public static double Pow(this double x, double k) => Math.Pow(x, k);

        public static int RoundToInt(this double x) => (int) Math.Round(x);
        public static long RoundToLong(this double x) => (long) Math.Round(x);

        internal static void ForEach<T>(this IList<T> source, Action<T> command)
        {
            foreach (var item in source)
            {
                command(item);
            }
        }
    }
}