using System;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Mathematics.Histograms;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Mathematics.Histograms
{
    public class MultimodalTests
    {
        private readonly ITestOutputHelper output;

        public MultimodalTests(ITestOutputHelper output) => this.output = output;

        [Theory]
        [InlineData(new double[] { 1, 1, 1, 1, 1, 1 }, 2)]
        [InlineData(new double[] { 1, 1, 1, 1, 1, 2, 2, 2, 2, 2 }, 4)]
        [InlineData(new double[] { 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3 }, 6)]
        [InlineData(new double[] { 1, 2, 3, 3, 3, 4, 5, 10, 11, 11, 11, 12, 40, 41, 41, 41, 42 }, 2.8333)]
        public void MValueTest(double[] values, double expectedMValue)
        {
            var s = new Statistics(values);
            var histogram = HistogramBuilder.Adaptive.Build(s);
            output.Print("Distribution", histogram);

            double acutalMValue = MathHelper.CalculateMValue(s);
            Assert.Equal(expectedMValue, acutalMValue, 4);
        }

        [Fact]
        public void RandomTest()
        {
            var random = new Random(42);
            double maxMValue = 0;
            int maxMValueN = 0;
            for (int n = 1; n <= 300; n++)
            {
                var values = new double[n];
                for (int i = 0; i < n; i++)
                    values[i] = random.NextGaussian(50, 3);
                
                var s = new Statistics(values);
                var histogram = HistogramBuilder.Adaptive.Build(s);
                output.Print($"n={n}", histogram);
                output.WriteLine("-------------------------------------------------------------------------");
                output.WriteLine("-------------------------------------------------------------------------");
                output.WriteLine("-------------------------------------------------------------------------");

                double mValue = MathHelper.CalculateMValue(s);
                Assert.True(mValue >= 2 - 1e-9);

                if (mValue > maxMValue)
                {
                    maxMValue = mValue;
                    maxMValueN = n;
                }
            }
            output.WriteLine($"maxMValue = {maxMValue} (N = {maxMValueN})");
        }
    }
}