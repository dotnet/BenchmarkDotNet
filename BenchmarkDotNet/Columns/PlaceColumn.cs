using System;
using System.Linq;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    // TODO: RomanNumber
    // TODO: Words (best, worst)
    public class PlaceColumn : IColumn
    {
        private enum Kind
        {
            ArabicNumber,
            Stars
        }

        private PlaceColumn(Kind kind)
        {
            this.kind = kind;
        }

        public static readonly IColumn ArabicNumber = new PlaceColumn(Kind.ArabicNumber);
        public static readonly IColumn Stars = new PlaceColumn(Kind.Stars);

        private readonly Kind kind;
        public string ColumnName => "Place";

        public string GetValue(Summary summary, Benchmark benchmark)
        {
            var sortedBenchmarks = summary.Benchmarks.
                OrderBy(b => summary[b].ResultStatistics.Mean).
                ToArray();
            var places = GetPlaces(sortedBenchmarks.Select(b => summary[b].ResultStatistics).ToArray());
            var place = places[Array.IndexOf(sortedBenchmarks, benchmark)];
            switch (kind)
            {
                case Kind.ArabicNumber:
                    return place.ToString();
                case Kind.Stars:
                    return new string('*', place);
            }
            return place.ToString();
        }

        private static int[] GetPlaces(Statistics[] s)
        {
            var n = s.Length;
            int[] places = new int[n];
            places[0] = 1;
            for (int i = 1; i < n; i++)
                if (LookSame(s[i - 1], s[i]))
                    places[i] = places[i - 1];
                else
                    places[i] = places[i - 1] + 1;
            return places;
        }

        private static bool LookSame(Statistics s1, Statistics s2) =>
            s1.Mean + 3*s1.StandardDeviation > s2.Mean &&
            s2.Mean - 3*s1.StandardDeviation < s1.Mean;

        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;
        public ColumnCategory Category => ColumnCategory.Custom;
        public override string ToString() => ColumnName;
    }
}