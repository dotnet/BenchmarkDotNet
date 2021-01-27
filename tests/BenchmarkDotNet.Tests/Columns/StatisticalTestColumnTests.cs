using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Mathematics;
using Perfolizer.Mathematics.SignificanceTesting;
using Perfolizer.Mathematics.Thresholds;
using BenchmarkDotNet.Reports;
using Xunit;

namespace BenchmarkDotNet.Tests.Columns
{
    public class StatisticalTestColumnTests
    {
        [Theory]
        [InlineData(StatisticalTestKind.MannWhitney, ThresholdUnit.Ratio, 0.05)]
        [InlineData(StatisticalTestKind.Welch, ThresholdUnit.Ratio, 0.05)]
        [InlineData(StatisticalTestKind.Welch, ThresholdUnit.Milliseconds, 10)]
        [InlineData(StatisticalTestKind.MannWhitney, ThresholdUnit.Milliseconds, 10)]
        public void NoDifferenceIfValuesAreTheSame(StatisticalTestKind statisticalTestKind, ThresholdUnit thresholdUnit, double thresholdValue)
        {
            var values = Enumerable.Repeat(100.0, 20).ToArray();

            Compare(statisticalTestKind, thresholdUnit, thresholdValue, values, values, "Base");
        }

        [Theory]
        [InlineData(StatisticalTestKind.MannWhitney, ThresholdUnit.Ratio, 0.02)]
        [InlineData(StatisticalTestKind.Welch, ThresholdUnit.Ratio, 0.02)]
        public void RegressionsAreDetected(StatisticalTestKind statisticalTestKind, ThresholdUnit thresholdUnit, double thresholdValue)
        {
            var baseline = new[] { 10.0, 10.01, 10.02, 10.0, 10.03, 10.02, 9.99, 9.98, 10.0, 10.02 };
            var current = baseline.Select(value => value * 1.03).ToArray();

            Compare(statisticalTestKind, thresholdUnit, thresholdValue, baseline, current, "Slower");
        }

        [Theory]
        [InlineData(StatisticalTestKind.MannWhitney, ThresholdUnit.Ratio, 0.02)]
        [InlineData(StatisticalTestKind.Welch, ThresholdUnit.Ratio, 0.02)]
        public void CanCompareDifferentSampleSizes(StatisticalTestKind statisticalTestKind, ThresholdUnit thresholdUnit, double thresholdValue)
        {
            var baseline = new[] { 10.0, 10.01, 10.02, 10.0, 10.03, 10.02, 9.99, 9.98, 10.0, 10.02 };
            var current = baseline
                .Skip(1) // we skip one element to make sure the sample size is different
                .Select(value => value * 1.03).ToArray();

            Compare(statisticalTestKind, thresholdUnit, thresholdValue, baseline, current, "Slower");
        }

        [Theory]
        [InlineData(StatisticalTestKind.MannWhitney, ThresholdUnit.Ratio, 0.02)]
        [InlineData(StatisticalTestKind.Welch, ThresholdUnit.Ratio, 0.02)]
        public void ImprovementsDetected(StatisticalTestKind statisticalTestKind, ThresholdUnit thresholdUnit, double thresholdValue)
        {
            var baseline = new[] { 10.0, 10.01, 10.02, 10.0, 10.03, 10.02, 9.99, 9.98, 10.0, 10.02 };
            var current = baseline.Select(value => value * 0.97).ToArray();

            Compare(statisticalTestKind, thresholdUnit, thresholdValue, baseline, current, "Faster");
        }

        private static void Compare(StatisticalTestKind statisticalTestKind, ThresholdUnit thresholdUnit, double thresholdValue, double[] baseline, double[] current, string expectedResult)
        {
            var sut = new StatisticalTestColumn(statisticalTestKind, Threshold.Create(thresholdUnit, thresholdValue));

            var emptyMetrics = new Dictionary<string, Metric>();

            Assert.Equal(expectedResult, sut.GetValue(null, null, new Statistics(baseline), emptyMetrics, new Statistics(current), emptyMetrics, isBaseline: true));
            Assert.Equal(expectedResult, sut.GetValue(null, null, new Statistics(baseline), emptyMetrics, new Statistics(current), emptyMetrics, isBaseline: false));
        }
    }
}