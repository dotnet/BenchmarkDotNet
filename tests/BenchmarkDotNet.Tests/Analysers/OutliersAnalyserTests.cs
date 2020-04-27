using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Mathematics;
using Perfolizer.Horology;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Analysers
{
    public class OutliersAnalyserTests
    {
        private readonly ITestOutputHelper output;

        public OutliersAnalyserTests(ITestOutputHelper output) => this.output = output;

        [Theory]
        [InlineData(0, 0, "")]
        [InlineData(1, 1, "1 outlier  was  removed")]
        [InlineData(2, 2, "2 outliers were removed")]
        [InlineData(3, 3, "3 outliers were removed")]
        [InlineData(0, 1, "1 outlier  was  detected")]
        [InlineData(0, 2, "2 outliers were detected")]
        [InlineData(0, 3, "3 outliers were detected")]
        [InlineData(1, 2, "1 outlier  was  removed, 2 outliers were detected")]
        [InlineData(2, 3, "2 outliers were removed, 3 outliers were detected")]
        public void SimpleMessageTest(int actualOutliers, int allOutliers, string expectedMessage)
        {
            string actualMessage = OutliersAnalyser.GetMessage(
                new double[actualOutliers], new double[allOutliers], Array.Empty<double>(), Array.Empty<double>(), TestCultureInfo.Instance);
            Assert.Equal(expectedMessage, actualMessage);
        }

        [Theory]
        [InlineData(0, 0, "")]
        [InlineData(0, 1, "1 outlier  was  removed (2.50 us)")]
        [InlineData(0, 2, "2 outliers were removed (2.50 us, 2.60 us)")]
        [InlineData(0, 3, "3 outliers were removed (2.50 us..2.70 us)")]
        [InlineData(1, 0, "1 outlier  was  removed (1.50 us)")]
        [InlineData(1, 1, "2 outliers were removed (1.50 us, 2.50 us)")]
        [InlineData(1, 2, "3 outliers were removed (1.50 us, 2.50 us, 2.60 us)")]
        [InlineData(1, 3, "4 outliers were removed (1.50 us, 2.50 us..2.70 us)")]
        [InlineData(2, 0, "2 outliers were removed (1.40 us, 1.50 us)")]
        [InlineData(2, 1, "3 outliers were removed (1.40 us, 1.50 us, 2.50 us)")]
        [InlineData(2, 2, "4 outliers were removed (1.40 us, 1.50 us, 2.50 us, 2.60 us)")]
        [InlineData(2, 3, "5 outliers were removed (1.40 us, 1.50 us, 2.50 us..2.70 us)")]
        [InlineData(3, 0, "3 outliers were removed (1.30 us..1.50 us)")]
        [InlineData(3, 1, "4 outliers were removed (1.30 us..1.50 us, 2.50 us)")]
        [InlineData(3, 2, "5 outliers were removed (1.30 us..1.50 us, 2.50 us, 2.60 us)")]
        [InlineData(3, 3, "6 outliers were removed (1.30 us..1.50 us, 2.50 us..2.70 us)")]
        public void RangeMessageTest(int lowerOutliers, int upperOutliers, string expectedMessage)
        {
            var values = new List<double>();
            for (int i = 0; i < 1000; i++)
                values.Add(2000);
            for (int i = 0; i < lowerOutliers; i++)
                values.Add(1500 - i * 100);
            for (int i = 0; i < upperOutliers; i++)
                values.Add(2500 + i * 100);
            values.Sort();
            var s = new Statistics(values);
            var cultureInfo = TestCultureInfo.Instance;
            string actualMessage = OutliersAnalyser.GetMessage(s.AllOutliers, s.AllOutliers, s.LowerOutliers, s.UpperOutliers, cultureInfo).ToAscii();
            output.WriteLine("Values   : " +
                             string.Join(", ", values.Take(5).Select(x =>  TimeInterval.FromNanoseconds(x).ToString(cultureInfo, "N2"))) +
                             ", ..., " +
                             string.Join(", ", values.Skip(values.Count - 5).Select(x => TimeInterval.FromNanoseconds(x).ToString(cultureInfo, "N2"))));
            output.WriteLine("Actual   : " + actualMessage);
            output.WriteLine("Expected : " + expectedMessage);
            Assert.Equal(expectedMessage, actualMessage);
        }
    }
}