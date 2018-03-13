using System;
using BenchmarkDotNet.Mathematics.Histograms;
using Xunit;

namespace BenchmarkDotNet.Tests.Mathematics.Histograms
{
    public class GeneralHistogramTests
    {
        [Fact]
        public void EmptyListTest()
        {
            foreach (var builder in HistogramBuilder.AllBuilders)
                Assert.Throws<ArgumentException>(() => builder.BuildWithFixedBinSize(Array.Empty<double>(), 1));
        }

        [Fact]
        public void NegativeBinSizeTest()
        {
            foreach (var builder in HistogramBuilder.AllBuilders)
                Assert.Throws<ArgumentException>(() => builder.BuildWithFixedBinSize(new double[] { 1, 2 }, -3));
        }
    }
}