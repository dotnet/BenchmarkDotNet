using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Horology;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Mathematics.Histograms
{
    [PublicAPI]
    public static class HistogramExtensions
    {
        [PublicAPI, Pure]
        public static int GetBinCount(this Histogram histogram) => histogram.Bins.Length;

        [PublicAPI, Pure, NotNull]
        public static IEnumerable<double> GetAllValues([NotNull] this Histogram histogram) => histogram.Bins.SelectMany(bin => bin.Values);

        [PublicAPI, Pure]
        public static string ToTimeStr(this Histogram histogram, TimeUnit unit = null, char binSymbol = '@', bool full = false)
        {
            const string format = "0.000";
            var bins = histogram.Bins;
            int binCount = histogram.Bins.Length;
            if (unit == null)
                unit = TimeUnit.GetBestTimeUnit(bins.SelectMany(bin => bin.Values).ToArray());

            var lower = new string[binCount];
            var upper = new string[binCount];
            for (int i = 0; i < binCount; i++)
            {
                lower[i] = bins[i].Lower.ToTimeStr(unit, format: format);
                upper[i] = bins[i].Upper.ToTimeStr(unit, format: format);
            }

            int lowerWidth = lower.Max(it => it.Length);
            int upperWidth = upper.Max(it => it.Length);

            var builder = new StringBuilder();
            for (int i = 0; i < binCount; i++)
            {
                string intervalStr = $"[{lower[i].PadLeft(lowerWidth)} ; {upper[i].PadLeft(upperWidth)})";
                string barStr = full
                    ? string.Join(", ", bins[i].Values.Select(it => it.ToTimeStr(unit, format: format)))
                    : new string(binSymbol, bins[i].Count);
                builder.AppendLine($"{intervalStr} | {barStr}");
            }

            return builder.ToString().Trim();
        }

        [PublicAPI, Pure]
        public static double GetOptimalBinSize([NotNull] this Statistics s, BinSizeRule? rule = null)
        {
            const BinSizeRule defaultRule = BinSizeRule.Scott2;
            switch (rule ?? defaultRule)
            {
                case BinSizeRule.FreedmanDiaconis:
                    return 2 * s.InterquartileRange / Math.Pow(s.N, 1.0 / 3);
                case BinSizeRule.Scott:
                    return 3.5 * s.StandardDeviation / Math.Pow(s.N, 1.0 / 3);
                case BinSizeRule.Scott2:
                    return 3.5 * s.StandardDeviation / Math.Pow(s.N, 1.0 / 3) / 2.0;
                case BinSizeRule.SquareRoot:
                    return (s.Max - s.Min) / Math.Sqrt(s.N);
                case BinSizeRule.Sturges:
                    return (s.Max - s.Min) / (Math.Ceiling(Math.Log(s.N, 2)) + 1);
                case BinSizeRule.Rice:
                    return (s.Max - s.Min) / (2 * Math.Pow(s.N, 1.0 / 3));
                default:
                    throw new ArgumentOutOfRangeException(nameof(rule), rule, null);
            }
        }
    }
}