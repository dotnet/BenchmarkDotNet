using System;
using System.Globalization;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Mathematics.Histograms
{
    [PublicAPI]
    public class Histogram
    {
        [PublicAPI]
        public double BinSize { get; }

        [PublicAPI, NotNull]
        public HistogramBin[] Bins { get; }

        [PublicAPI, Pure]
        public string ToString(Func<double, string> formatter, char binSymbol = '@', bool full = false)
        {
            var lower = new string[Bins.Length];
            var upper = new string[Bins.Length];
            for (int i = 0; i < Bins.Length; i++)
            {
                lower[i] = formatter(Bins[i].Lower);
                upper[i] = formatter(Bins[i].Upper);
            }

            int lowerWidth = lower.Max(it => it.Length);
            int upperWidth = upper.Max(it => it.Length);

            var builder = new StringBuilder();
            for (int i = 0; i < Bins.Length; i++)
            {
                string intervalStr = $"[{lower[i].PadLeft(lowerWidth)} ; {upper[i].PadLeft(upperWidth)})";
                string barStr = full
                    ? string.Join(", ", Bins[i].Values.Select(formatter))
                    : new string(binSymbol, Bins[i].Count);
                builder.AppendLine($"{intervalStr} | {barStr}");
            }

            return builder.ToString().Trim();
        }

        public override string ToString() => ToString(x => x.ToString("0.000", DateTimeFormatInfo.CurrentInfo));

        internal Histogram(double binSize, [NotNull] HistogramBin[] bins)
        {
            BinSize = binSize;
            Bins = bins;
        }

        // For unit tests
        [Pure, NotNull]
        internal static Histogram BuildManual(double binSize, [NotNull] double[][] bins)
        {
            var histogramBins = bins.Select(bin => new HistogramBin(bin.Any() ? bin.Min() : 0, bin.Any() ? bin.Max() : 0, bin)).ToArray();
            return new Histogram(binSize, histogramBins);
        }
    }
}