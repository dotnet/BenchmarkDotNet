using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;
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
        
        public static Func<double, string> CreateNanosecondFormatter(this Histogram histogram, CultureInfo cultureInfo = null, string format = "0.000")
        {
            var timeUnit = TimeUnit.GetBestTimeUnit(histogram.Bins.SelectMany(bin => bin.Values).ToArray());
            return value => TimeInterval.FromNanoseconds(value).ToString(timeUnit, cultureInfo, format);
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