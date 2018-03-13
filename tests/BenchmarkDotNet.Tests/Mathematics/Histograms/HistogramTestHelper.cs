using System.Linq;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Mathematics.Histograms;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Mathematics.Histograms
{
    public static class HistogramTestHelper
    {
        [AssertionMethod]
        public static void DoHistogramTest([NotNull] ITestOutputHelper output, [NotNull] IHistogramBuilder builder,
            [NotNull] double[] values, [NotNull] double[][] bins)
        {
            var actualHistogram = builder.Build(new Statistics(values));
            Check(output, bins, actualHistogram);
        }

        [AssertionMethod]
        public static void DoHistogramTest([NotNull] ITestOutputHelper output, [NotNull] IHistogramBuilder builder,
            [NotNull] double[] values, double binSize, [NotNull] double[][] bins)
        {
            var actualHistogram = builder.BuildWithFixedBinSize(values, binSize);
            Check(output, bins, actualHistogram);
        }
        
        [AssertionMethod]
        public static void DoHistogramTest([NotNull] ITestOutputHelper output, [NotNull] IHistogramBuilder builder,
            [NotNull] double[] values, [NotNull] bool[] states)
        {
            var actualHistogram = builder.Build(new Statistics(values));
            Check(output, states, actualHistogram);
        }

        [AssertionMethod]
        private static void Check([NotNull] ITestOutputHelper output, [NotNull] double[][] expectedBins, Histogram actualHistogram)
        {
            var expectedHistogram = Histogram.BuildManual(0, expectedBins);
            output.Print("Expected", expectedHistogram);
            output.Print("Actual", actualHistogram);

            Assert.Equal(expectedBins.Length, actualHistogram.Bins.Length);
            for (int i = 0; i < actualHistogram.Bins.Length; i++)
                Assert.Equal(expectedBins[i], actualHistogram.Bins[i].Values);
        }
        
        [AssertionMethod]
        private static void Check([NotNull] ITestOutputHelper output, [NotNull] bool[] expectedStates, Histogram actualHistogram)
        {
            output.Print("Actual", actualHistogram);

            Assert.Equal(expectedStates.Length, actualHistogram.Bins.Length);
            for (int i = 0; i < actualHistogram.Bins.Length; i++)
                Assert.Equal(expectedStates[i], actualHistogram.Bins[i].HasAny);
        }

        public static void Print([NotNull] this ITestOutputHelper output, [NotNull] string title, [NotNull] Histogram histogram)
        {
            var s = new Statistics(histogram.GetAllValues());
            double mValue = MathHelper.CalculateMValue(s);
            output.WriteLine($"=== {title}:Short (BinSize={histogram.BinSize.ToTimeStr()}, mValue={mValue.ToStr()}) ===");
            output.WriteLine(histogram.ToTimeStr());
            output.WriteLine($"=== {title}:Full (BinSize={histogram.BinSize.ToTimeStr()}, mValue={mValue.ToStr()}) ===");
            output.WriteLine(histogram.ToTimeStr(full: true));
            output.WriteLine("OUTLIERS: ", string.Join(", ", s.Outliers.Select(it => it.ToTimeStr())));
        }
    }
}