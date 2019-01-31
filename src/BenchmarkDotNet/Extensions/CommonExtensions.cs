﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Horology;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Extensions
{
    internal static class CommonExtensions
    {
        public static string ToTimeStr(this double value, TimeUnit unit = null, int unitNameWidth = 1, bool showUnit = true, string format = "N4",
            Encoding encoding = null)
        {
            unit = unit ?? TimeUnit.GetBestTimeUnit(value);
            double unitValue = TimeUnit.Convert(value, TimeUnit.Nanosecond, unit);
            if (showUnit)
            {
                string unitName = unit.Name.ToString(encoding ?? Encoding.ASCII).PadLeft(unitNameWidth);
                return $"{unitValue.ToStr(format)} {unitName}";
            }

            return $"{unitValue.ToStr(format)}";
        }

        public static string ToTimeStr(this double value, TimeUnit unit, Encoding encoding, string format = "N4", int unitNameWidth = 1, bool showUnit = true)
            => value.ToTimeStr(unit, unitNameWidth, showUnit, format, encoding);

        public static string ToTimeStr(this double value, Encoding encoding, TimeUnit unit = null, string format = "N4", int unitNameWidth = 1,
            bool showUnit = true)
            => value.ToTimeStr(unit, unitNameWidth, showUnit, format, encoding);

        public static string ToSizeStr(this long value, SizeUnit unit = null, int unitNameWidth = 1, bool showUnit = true)
        {
            unit = unit ?? SizeUnit.GetBestSizeUnit(value);
            double unitValue = SizeUnit.Convert(value, SizeUnit.B, unit);
            if (showUnit)
            {
                string unitName = unit.Name.PadLeft(unitNameWidth);
                return string.Format(HostEnvironmentInfo.MainCultureInfo, "{0:0.##} {1}", unitValue, unitName);
            }

            return string.Format(HostEnvironmentInfo.MainCultureInfo, "{0:0.##}", unitValue);
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
        public static string GetColumnTitle(this IColumn column, SummaryStyle style)
        {
            if (!style.PrintUnitsInHeader)
                return column.ColumnName;

            switch (column.UnitType)
            {
                case UnitType.Size:
                    return $"{column.ColumnName} [{style.SizeUnit.Name}]";
                case UnitType.Time:
                    return $"{column.ColumnName} [{style.TimeUnit.Name}]";
                case UnitType.Dimensionless:
                    return column.ColumnName;
                default:
                    return column.ColumnName;
            }
        }

        public static bool IsNullOrEmpty<T>(this IReadOnlyCollection<T> value) => value == null || value.Count == 0;
        public static bool IsEmpty<T>(this IReadOnlyCollection<T> value) => value.Count == 0;
        public static bool IsEmpty<T>(this IEnumerable<T> value) => !value.Any();

        public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> collection)
        {
            foreach (var item in collection)
                hashSet.Add(item);
        }
        
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            => dictionary.TryGetValue(key, out var value) ? value : default;

        public static double Sqr(this double x) => x * x;
        public static double Pow(this double x, double k) => Math.Pow(x, k);

        internal static IEnumerable<TItem> DistinctBy<TItem, TValue>(this IEnumerable<TItem> items, Func<TItem, TValue> selector)
            => DistinctBy(items, selector, EqualityComparer<TValue>.Default);

        private static IEnumerable<TItem> DistinctBy<TItem, TValue>(this IEnumerable<TItem> items, Func<TItem, TValue> selector,
            IEqualityComparer<TValue> equalityComparer)
        {
            var seen = new HashSet<TValue>(equalityComparer);

            foreach (var item in items)
                if (seen.Add(selector(item)))
                    yield return item;
        }

        internal static void ForEach<T>(this IList<T> source, Action<T> command)
        {
            foreach (var item in source)
                command(item);
        }

        internal static string CreateIfNotExists(this string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);

            return directoryPath;
        }

        internal static bool IsNotNullButDoesNotExist(this FileSystemInfo fileInfo)
            => fileInfo != null && !fileInfo.Exists;
    }
}