using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Reports;

namespace BenchmarkDotNet.Extensions
{
    internal static class CommonExtensions
    {
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
            var unitValue = TimeUnit.Convert(value, TimeUnit.Nanoseconds, unit);
            var unitName = unit.Name.PadLeft(unitNameWidth);
            return $"{unitValue.ToStr("N4")} {unitName}";
        }

        public static string ToStr(this double value, string format = "0.##") =>
            string.Format(EnvironmentHelper.MainCultureInfo, $"{{0:{format}}}", value);

        public static bool IsNullOrEmpty<T>(this IList<T> value) => value == null || value.Count == 0;
        public static bool IsEmpty<T>(this IList<T> value) => value.Count == 0;

        public static bool IsOneOf<T>(this T value, params T[] values) => values.Contains(value);
    }
}