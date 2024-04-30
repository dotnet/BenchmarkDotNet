using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Reports;
using Perfolizer.Mathematics.Distributions.ContinuousDistributions;
using Perfolizer.Metrology;
using Xunit;

namespace BenchmarkDotNet.Tests.Columns
{
    public class StatisticalTestColumnTests
    {
        [Theory]
        [InlineData("5%")]
        [InlineData("10ms")]
        public void NoDifferenceIfValuesAreTheSame(string threshold)
        {
            double[] values = Enumerable.Repeat(100.0, 20).ToArray();
            Compare(threshold, values, values, "Baseline");
        }

        [Theory]
        [InlineData("2%")]
        public void RegressionsAreDetected(string threshold)
        {
            double[]? baseline = [10.0, 10.01, 10.02, 10.0, 10.03, 10.02, 9.99, 9.98, 10.0, 10.02];
            double[]? current = baseline.Select(value => value * 1.03).ToArray();

            Compare(threshold, baseline, current, "Slower");
        }

        [Theory]
        [InlineData("2%")]
        public void CanCompareDifferentSampleSizes(string threshold)
        {
            double[] baseline = new NormalDistribution(10, 0.01).Random(1729).Next(30);
            double[] current = baseline
                .Skip(1) // we skip one element to make sure the sample size is different
                .Select(value => value * 1.03).ToArray();

            Compare(threshold, baseline, current, "Slower");
        }

        [Theory]
        [InlineData("2%")]
        public void ImprovementsDetected(string threshold)
        {
            var baseline = new[] { 10.0, 10.01, 10.02, 10.0, 10.03, 10.02, 9.99, 9.98, 10.0, 10.02 };
            var current = baseline.Select(value => value * 0.97).ToArray();

            Compare(threshold, baseline, current, "Faster");
        }

        private static void Compare(string threshold, double[] baseline, double[] current, string expectedResult)
        {
            var sut = new StatisticalTestColumn(Threshold.Parse(threshold));

            var emptyMetrics = new Dictionary<string, Metric>();

            Assert.Equal(expectedResult,
                sut.GetValue(null, null, new Statistics(baseline), emptyMetrics, new Statistics(current), emptyMetrics, isBaseline: true));
            Assert.Equal(expectedResult,
                sut.GetValue(null, null, new Statistics(baseline), emptyMetrics, new Statistics(current), emptyMetrics, isBaseline: false));
        }
    }
}