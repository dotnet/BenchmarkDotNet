using System;
using System.Linq;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    public class RankColumn : IColumn
    {
        public enum Kind
        {
            Arabic,
            Roman,
            Stars,
            Words
        }

        public RankColumn(Kind kind)
        {
            this.kind = kind;
        }

        public static readonly IColumn Arabic = new RankColumn(Kind.Arabic);
        public static readonly IColumn Roman = new RankColumn(Kind.Roman);
        public static readonly IColumn Stars = new RankColumn(Kind.Stars);
        public static readonly IColumn Words = new RankColumn(Kind.Words);

        private readonly Kind kind;
        public string ColumnName => "Rank";

        public string GetValue(Summary summary, Benchmark benchmark)
        {
            var sortedBenchmarks = summary.Benchmarks.
                OrderBy(b => summary[b].ResultStatistics.Mean).
                ToArray();
            var places = GetRanks(sortedBenchmarks.Select(b => summary[b].ResultStatistics).ToArray());
            int place = places[Array.IndexOf(sortedBenchmarks, benchmark)];
            switch (kind)
            {
                case Kind.Arabic:
                    return place.ToString();
                case Kind.Roman:
                    return ToRoman(place);
                case Kind.Stars:
                    return new string('*', place);
                case Kind.Words:
                {
                    if (place == 1)
                        return "Best";
                    if (place == places.Max())
                        return "Worst";
                    return "Medium";
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool IsDefault(Summary summary, Benchmark benchmark) => false;

        private static int[] GetRanks(Statistics[] s)
        {
            int n = s.Length;
            var ranks = new int[n];
            ranks[0] = 1;
            for (int i = 1; i < n; i++)
                if (LookSame(s[i - 1], s[i]))
                    ranks[i] = ranks[i - 1];
                else
                    ranks[i] = ranks[i - 1] + 1;
            return ranks;
        }

        // TODO: Improve
        private static bool LookSame(Statistics s1, Statistics s2) =>
            s1.Mean + 3 * s1.StandardDeviation > s2.Mean &&
            s2.Mean - 3 * s1.StandardDeviation < s1.Mean;

        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public override string ToString() => ColumnName;

        // Based on http://stackoverflow.com/questions/7040289/converting-integers-to-roman-numerals
        private static string ToRoman(int number)
        {
            if ((number < 0) || (number > 3999)) throw new ArgumentOutOfRangeException(nameof(number), "insert value betwheen 1 and 3999");
            if (number < 1) return string.Empty;
            if (number >= 1000) return "M" + ToRoman(number - 1000);
            if (number >= 900) return "CM" + ToRoman(number - 900);
            if (number >= 500) return "D" + ToRoman(number - 500);
            if (number >= 400) return "CD" + ToRoman(number - 400);
            if (number >= 100) return "C" + ToRoman(number - 100);
            if (number >= 90) return "XC" + ToRoman(number - 90);
            if (number >= 50) return "L" + ToRoman(number - 50);
            if (number >= 40) return "XL" + ToRoman(number - 40);
            if (number >= 10) return "X" + ToRoman(number - 10);
            if (number >= 9) return "IX" + ToRoman(number - 9);
            if (number >= 5) return "V" + ToRoman(number - 5);
            if (number >= 4) return "IV" + ToRoman(number - 4);
            if (number >= 1) return "I" + ToRoman(number - 1);
            throw new ArgumentOutOfRangeException(nameof(number), "something bad happened");
        }
    }
}