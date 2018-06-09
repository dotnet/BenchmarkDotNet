using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using System.IO;
using System.Text;

namespace BenchmarkDotNet.Extensions
{
    public static class CommonExtensions
    {
        public static List<T> ToSortedList<T>(this IEnumerable<T> values)
        {
            var list = new List<T>();
            list.AddRange(values);
            list.Sort();
            return list;
        }

        public static string ToTimeStr(this double value, TimeUnit unit = null, int unitNameWidth = 1, bool showUnit = true, string format = "N4", Encoding encoding = null)
        {
            unit = unit ?? TimeUnit.GetBestTimeUnit(value);
            var unitValue = TimeUnit.Convert(value, TimeUnit.Nanosecond, unit);
            if (showUnit)
            {
                var unitName = unit.Name.ToString(encoding ?? Encoding.ASCII).PadLeft(unitNameWidth);
                return $"{unitValue.ToStr(format)} {unitName}";
            }

            return $"{unitValue.ToStr(format)}";
        }
        
        public static string ToTimeStr(this double value, TimeUnit unit, Encoding encoding, string format = "N4", int unitNameWidth = 1, bool showUnit = true)
            => value.ToTimeStr(unit, unitNameWidth, showUnit, format, encoding);
        
        public static string ToTimeStr(this double value, Encoding encoding, TimeUnit unit = null, string format = "N4", int unitNameWidth = 1, bool showUnit = true)
            => value.ToTimeStr(unit, unitNameWidth, showUnit, format, encoding);

        public static string ToSizeStr(this long value, SizeUnit unit = null, int unitNameWidth = 1, bool showUnit = true)
        {
            unit = unit ?? SizeUnit.GetBestSizeUnit(value);
            var unitValue = SizeUnit.Convert(value, SizeUnit.B, unit);
            if (showUnit)
            {
                var unitName = unit.Name.PadLeft(unitNameWidth);
                return string.Format(HostEnvironmentInfo.MainCultureInfo, "{0:0.##} {1}", unitValue, unitName);
            }
            else
            {
                return string.Format(HostEnvironmentInfo.MainCultureInfo, "{0:0.##}", unitValue);
            }
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

        /// <summary>
        /// Gets column title formatted using the specified style
        /// </summary>
        public static string GetColumnTitle(this IColumn column, ISummaryStyle style)
        {
            if (!style.PrintUnitsInHeader)
                return column.ColumnName;

            switch (column.UnitType)
            {
                case UnitType.Size:
                    return $"{column.ColumnName} [{style.SizeUnit.Name}]";
                case UnitType.Time:
                    return $"{column.ColumnName} [{style.TimeUnit.Name}]";
                default:
                    return column.ColumnName;
            }
        }

        public static bool IsNullOrEmpty<T>(this IReadOnlyCollection<T> value) => value == null || value.Count == 0;
        public static bool IsEmpty<T>(this IReadOnlyCollection<T> value) => value.Count == 0;
        public static bool IsEmpty<T>(this IEnumerable<T> value) => !value.Any();

        public static T Penult<T>(this IList<T> list) => list[list.Count - 2];

        public static bool IsOneOf<T>(this T value, params T[] values) => values.Contains(value);

#if !NETCOREAPP2_1 // method with the same name was added to Dictionary in .NET Core 2.1, so we need this ulgy hack to get compiler happy
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            => dictionary.TryGetValue(key, out TValue value) ? value : default;
#endif
        public static double Sqr(this double x) => x * x;
        public static double Pow(this double x, double k) => Math.Pow(x, k);

        public static int RoundToInt(this double x) => (int) Math.Round(x);
        public static long RoundToLong(this double x) => (long) Math.Round(x);

        internal static IEnumerable<TItem> DistinctBy<TItem, TValue>(this IEnumerable<TItem> items, Func<TItem, TValue> selector)
            => DistinctBy(items, selector, EqualityComparer<TValue>.Default);

        internal static IEnumerable<TItem> DistinctBy<TItem, TValue>(this IEnumerable<TItem> items, Func<TItem, TValue> selector, IEqualityComparer<TValue> equalityComparer)
        {
            var seen = new HashSet<TValue>(equalityComparer);

            foreach (var item in items)
            {
                if (seen.Add(selector(item)))
                    yield return item;
            }
        }

        internal static void ForEach<T>(this IList<T> source, Action<T> command)
        {
            foreach (var item in source)
            {
                command(item);
            }
        }

        internal static string CreateIfNotExists(this string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            return directoryPath;
        }
    }
}