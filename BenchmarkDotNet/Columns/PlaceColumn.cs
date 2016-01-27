using System;
using System.Linq;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace BenchmarkDotNet.Columns
{
    // TODO: RomanNumber
    // TODO: Words (best, worst)
    public class PlaceColumn : IColumn
    {
        public static readonly IColumn ArabicNumber = new PlaceColumn();

        public string ColumnName => "Place";

        public string GetValue(Summary summary, Benchmark benchmark)
        {
            var sortedBenchmarks = summary.Benchmarks.
                OrderBy(b => summary.Reports[b].TargetStatistics.Mean).
                ToArray();
            var places = GetPlaces(sortedBenchmarks.Select(b => summary.Reports[b].TargetStatistics).ToArray());
            return places[Array.IndexOf(sortedBenchmarks, benchmark)].ToString();
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
            s1.Mean + 3 * s1.StandardDeviation > s2.Mean &&
            s2.Mean - 3 * s1.StandardDeviation < s1.Mean;

        public bool IsAvailable(Summary summary) => true;
        public bool AlwaysShow => true;
        public override string ToString() => ColumnName;
    }
}