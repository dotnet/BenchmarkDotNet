﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Extensions
{
    internal static class CommonExtensions
    {
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