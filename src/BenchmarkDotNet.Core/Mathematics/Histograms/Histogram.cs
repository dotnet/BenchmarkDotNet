using System.Linq;
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