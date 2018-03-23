using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Extensions;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Mathematics.Histograms
{
    internal class SimpleHistogramBuilder : IHistogramBuilder    
    {
        [PublicAPI, Pure]
        public Histogram Build(Statistics s, BinSizeRule? rule = null)
        {
            double binSize = s.GetOptimalBinSize(rule);
            return BuildWithFixedBinSize(s.GetValues(), binSize);
        }

        [PublicAPI, Pure]
        public Histogram BuildWithFixedBinSize(IEnumerable<double> values, double binSize)
        {
                if (binSize < 1e-9)
                    throw new ArgumentException($"binSize ({binSize.ToStr()}) should be a positive number", nameof(binSize));
    
                var list = values.ToList();
                if (list.IsEmpty())
                    throw new ArgumentException("Values should be non-empty", nameof(values));
    
                list.Sort();

            int firstBin = GetBinIndex(list.First(), binSize);
            int lastBin = GetBinIndex(list.Last(), binSize);
            int binCount = lastBin - firstBin + 1;

            var bins = new HistogramBin[binCount];
            int counter = 0;
            for (int i = 0; i < bins.Length; i++)
            {
                var bin = new List<double>();
                double lower = (firstBin + i) * binSize;
                double upper = (firstBin + i + 1) * binSize;

                while (counter < list.Count && (list[counter] < upper || i == bins.Length - 1))
                    bin.Add(list[counter++]);

                bins[i] = new HistogramBin(lower, upper, bin.ToArray());
            }

            return new Histogram(binSize, bins);
        }

        private static int GetBinIndex(double value, double binSize) => (int) Math.Floor(value / binSize);

    }
}