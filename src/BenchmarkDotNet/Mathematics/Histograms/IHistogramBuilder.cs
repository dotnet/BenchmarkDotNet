using System.Collections.Generic;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Mathematics.Histograms
{
    public interface IHistogramBuilder
    {
        [PublicAPI, Pure, NotNull]
        Histogram Build(Statistics s, BinSizeRule? rule = null);
        
        [PublicAPI, Pure, NotNull]
        Histogram BuildWithFixedBinSize(IEnumerable<double> values, double binSize);
    }
}