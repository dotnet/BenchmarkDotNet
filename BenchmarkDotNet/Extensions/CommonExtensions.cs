using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BenchmarkDotNet.Extensions
{
    internal static class CommonExtensions
    {
        public static IEnumerable<T> TakeLast<T>(this IList<T> source, int count)
        {
            return source.Skip(Math.Max(0, source.Count - count));
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

        public static string ToStr(this double value, string format = "0.##") => string.Format(EnvironmentInfo.MainCultureInfo, $"{{0:{format}}}", value);

        public static bool IsEmpty<T>(this IList<T> value) => value.Count == 0;
    }
}