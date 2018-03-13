using System;
using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Mathematics.Histograms;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Mathematics
{
    public class HistogramTests
    {
        private readonly ITestOutputHelper output;

        public HistogramTests(ITestOutputHelper output) => this.output = output;

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

        [Fact]
        public void SimpeHistogramTest1()
        {
            DoSimpleHistogramTest(new[] { 1.0, 2.0, 3.0, 4.0, 5.0 }, 1,
                new[]
                {
                    new[] { 1.0 },
                    new[] { 2.0 },
                    new[] { 3.0 },
                    new[] { 4.0 },
                    new[] { 5.0 }
                });
        }

        [Fact]
        public void SimpeHistogramTest2()
        {
            DoSimpleHistogramTest(new[] { 1.0, 2.0, 3.0, 4.0, 5.0 }, 2.5,
                new[]
                {
                    new[] { 1.0, 2.0 },
                    new[] { 3.0, 4.0 },
                    new[] { 5.0 }
                });
        }

        [Fact]
        public void SimpeHistogramTest3()
        {
            DoSimpleHistogramTest(new[] { 1.0, 1.1, 1.2, 1.3, 1.4, 1.5, 2.7 }, 2.0,
                new[]
                {
                    new[] { 1.0, 1.1, 1.2, 1.3, 1.4, 1.5 },
                    new[] { 2.7 }
                });
        }

        private void DoSimpleHistogramTest(double[] values, double binSize, double[][] bins)
        {
            var expectedHistogram = Histogram.BuildManual(binSize, bins);
            var actualHistogram = HistogramBuilder.Simple.BuildWithFixedBinSize(values, binSize);
            PrintHistogram("Expected", expectedHistogram);
            PrintHistogram("Actual", actualHistogram);

            Assert.Equal(bins.Length, actualHistogram.Bins.Length);
            for (int i = 0; i < actualHistogram.Bins.Length; i++)
                Assert.Equal(bins[i], actualHistogram.Bins[i].Values);
        }

        private void PrintHistogram(string title, Histogram histogram)
        {
            output.WriteLine($"=== {title}:Short ===");
            output.WriteLine(histogram.ToTimeStr());
            output.WriteLine($"=== {title}:Full ===");
            output.WriteLine(histogram.ToTimeStr(full: true));
        }
        
        [Theory]
        [InlineData(new[] { 1.0, 2.0, 3.0 })]
        [InlineData(new[] { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0 })]
        [InlineData(new[] { 0.0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9 })]
        [InlineData(new[] { 1.0, 1, 1, 2, 2, 2, 3, 3, 3 })]
        [InlineData(new[] { 100.0, 100, 100, 200, 200, 200, 300, 300, 300 })]
        [InlineData(new[] { 1.0, 1.01, 1.02, 1.03, 1.03, 1.04, 1.05, 1.01, 1.02, 1.03, 1.02 })]
        private void BinSizeTest(double[] values)
        {
            var rules = Enum.GetValues(typeof(BinSizeRule)).Cast<BinSizeRule>();
            var s = new Statistics(values);
            foreach (var rule in rules)
            {
                var histogram = HistogramBuilder.Simple.Build(s, rule);
                output.WriteLine($"!!!!! Rule = {rule}, BinSize = {histogram.BinSize.ToTimeStr()} !!!!!");
                output.WriteLine(histogram.ToTimeStr());
                output.WriteLine("");
                output.WriteLine("");
            }
        }
    }
}