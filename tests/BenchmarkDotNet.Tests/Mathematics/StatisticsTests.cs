using System;
using System.Linq;
using BenchmarkDotNet.Mathematics;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Mathematics
{
    public class StatisticsTests
    {
        private readonly ITestOutputHelper output;

        public StatisticsTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        private void Print(Statistics summary)
        {
            output.WriteLine("Min = " + summary.Min);
            output.WriteLine("LowerFence = " + summary.LowerFence);
            output.WriteLine("Q1 = " + summary.Q1);
            output.WriteLine("Median = " + summary.Median);
            output.WriteLine("Mean = " + summary.Mean);
            output.WriteLine("Q3 = " + summary.Q3);
            output.WriteLine("UpperFence = " + summary.UpperFence);
            output.WriteLine("Max = " + summary.Max);
            output.WriteLine("InterquartileRange = " + summary.InterquartileRange);
            output.WriteLine("StandardDeviation = " + summary.StandardDeviation);
            output.WriteLine("Outlier = [" + string.Join("; ", summary.Outliers) + "]");
            output.WriteLine("CI = " + summary.ConfidenceInterval.ToStr());
            output.WriteLine("Percentiles = " + summary.Percentiles.ToStr());
        }

        [Fact]
        public void Test0()
        {
            Assert.Throws<InvalidOperationException>(() => new Statistics());
        }

        [Fact]
        public void Test1()
        {
            var summary = new Statistics(1);
            Print(summary);
            Assert.Equal(1, summary.Min);
            Assert.Equal(1, summary.LowerFence);
            Assert.Equal(1, summary.Q1);
            Assert.Equal(1, summary.Median);
            Assert.Equal(1, summary.Mean);
            Assert.Equal(1, summary.Q3);
            Assert.Equal(1, summary.UpperFence);
            Assert.Equal(1, summary.Max);
            Assert.Equal(0, summary.InterquartileRange);
            Assert.Equal(0, summary.StandardDeviation);
            Assert.Equal(new double[0], summary.Outliers);
            Assert.Equal(1, summary.Percentiles.P0);
            Assert.Equal(1, summary.Percentiles.P25);
            Assert.Equal(1, summary.Percentiles.P50);
            Assert.Equal(1, summary.Percentiles.P85);
            Assert.Equal(1, summary.Percentiles.P95);
            Assert.Equal(1, summary.Percentiles.P100);
        }

        [Fact]
        public void Test2()
        {
            var summary = new Statistics(1, 2);
            Print(summary);
            Assert.Equal(1, summary.Min);
            Assert.Equal(-0.5, summary.LowerFence);
            Assert.Equal(1, summary.Q1);
            Assert.Equal(1.5, summary.Median);
            Assert.Equal(1.5, summary.Mean);
            Assert.Equal(2, summary.Q3);
            Assert.Equal(3.5, summary.UpperFence);
            Assert.Equal(2, summary.Max);
            Assert.Equal(1, summary.InterquartileRange);
            Assert.Equal(0.70711, summary.StandardDeviation, 4);
            Assert.Equal(new double[0], summary.Outliers);
			Assert.Equal(1, summary.Percentiles.P0);
			Assert.Equal(1.25, summary.Percentiles.P25);
			Assert.Equal(1.5, summary.Percentiles.P50);
			Assert.Equal(1.85, summary.Percentiles.P85);
			Assert.Equal(1.95, summary.Percentiles.P95);
			Assert.Equal(2, summary.Percentiles.P100);
        }

        [Fact]
        public void Test3()
        {
            var summary = new Statistics(1, 2, 4);
            Print(summary);
            Assert.Equal(1, summary.Min);
            Assert.Equal(-3.5, summary.LowerFence);
            Assert.Equal(1, summary.Q1);
            Assert.Equal(2, summary.Median);
            Assert.Equal(2.333333, summary.Mean, 5);
            Assert.Equal(4, summary.Q3);
            Assert.Equal(8.5, summary.UpperFence);
            Assert.Equal(4, summary.Max);
            Assert.Equal(3, summary.InterquartileRange);
            Assert.Equal(1.52753, summary.StandardDeviation, 4);
            Assert.Equal(new double[0], summary.Outliers);
			Assert.Equal(1, summary.Percentiles.P0);
			Assert.Equal(1.5, summary.Percentiles.P25);
			Assert.Equal(2, summary.Percentiles.P50);
			Assert.Equal(3.4, summary.Percentiles.P85);
			Assert.Equal(3.8, summary.Percentiles.P95);
			Assert.Equal(4, summary.Percentiles.P100);
        }

        [Fact]
        public void Test7()
        {
            var summary = new Statistics(1, 2, 4, 8, 16, 32, 64);
            Print(summary);
            Assert.Equal(1, summary.Min);
            Assert.Equal(-43, summary.LowerFence);
            Assert.Equal(2, summary.Q1);
            Assert.Equal(8, summary.Median);
            Assert.Equal(18.1428571429, summary.Mean, 5);
            Assert.Equal(32, summary.Q3);
            Assert.Equal(77, summary.UpperFence);
            Assert.Equal(64, summary.Max);
            Assert.Equal(30, summary.InterquartileRange);
            Assert.Equal(22.9378, summary.StandardDeviation, 4);
            Assert.Equal(new double[0], summary.Outliers);
			Assert.Equal(1, summary.Percentiles.P0);
			Assert.Equal(3, summary.Percentiles.P25);
			Assert.Equal(8, summary.Percentiles.P50);
			Assert.Equal(35.2, summary.Percentiles.P85, 4);
			Assert.Equal(54.4, summary.Percentiles.P95, 4);
			Assert.Equal(64, summary.Percentiles.P100);
        }

        [Fact]
        public void OutlierTest()
        {
            var summary = new Statistics(1, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 10, 10.1);
            Print(summary);
            Assert.Equal(new[] { 10, 10.1 }, summary.Outliers);
        }

        [Fact]
        public void ConfidenceIntervalTest()
        {
            var summary = new Statistics(Enumerable.Range(1, 30));
            Print(summary);
            Assert.Equal(95, summary.ConfidenceInterval.Level.ToPercent());
            Assert.Equal(15.5, summary.ConfidenceInterval.Mean);
            Assert.Equal(summary.StandardError, summary.ConfidenceInterval.Error);
            Assert.Equal(12.34974, summary.ConfidenceInterval.Lower, 4);
            Assert.Equal(18.65026, summary.ConfidenceInterval.Upper, 4);
        }

        [Fact]
        public void PercentileValuesTest()
        {
            var summary = new Statistics(Enumerable.Range(1, 30));
            Print(summary);
            Assert.Equal(1, summary.Percentiles.P0);
            Assert.Equal(8.25, summary.Percentiles.P25);
            Assert.Equal(15.5, summary.Percentiles.P50);
            Assert.Equal(20.43, summary.Percentiles.P67, 4);
            Assert.Equal(24.2, summary.Percentiles.P80, 4);
            Assert.Equal(25.65, summary.Percentiles.P85);
            Assert.Equal(27.1, summary.Percentiles.P90);
            Assert.Equal(28.55, summary.Percentiles.P95, 4);
            Assert.Equal(30, summary.Percentiles.P100);

            var a = Enumerable.Range(1, 30);
            var b = Enumerable.Repeat(0, 30).Concat(a);
            var c = b.Concat(Enumerable.Repeat(31, 30));
            summary = new Statistics(c);
            Print(summary);
            Assert.Equal(0, summary.Percentiles.P0);
            Assert.Equal(0, summary.Percentiles.P25);
            Assert.Equal(15.5, summary.Percentiles.P50);
            Assert.Equal(30.63, summary.Percentiles.P67, 4);
            Assert.Equal(31, summary.Percentiles.P80);
            Assert.Equal(31, summary.Percentiles.P85);
            Assert.Equal(31, summary.Percentiles.P90);
            Assert.Equal(31, summary.Percentiles.P95);
            Assert.Equal(31, summary.Percentiles.P100);
        }
    }
}