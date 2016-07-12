using System;
using System.Linq;
using BenchmarkDotNet.Mathematics;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests
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
            var b = Enumerable.Concat(Enumerable.Repeat(0, 30), a);
            var c = Enumerable.Concat(b, Enumerable.Repeat(31, 30));
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

        [Fact]
        public void Welch()
        {
            // R-script for validation:
            // set.seed(42); x <- rnorm(30, mean = 10)
            // set.seed(42); y <- rnorm(40, mean = 10.1)
            // t.test(x, y)
            //
            // #     Welch Two Sample t-test
            // # 
            // # data:  x and y
            // # t = 0.027097, df = 61.716, p-value = 0.9785
            // # alternative hypothesis: true difference in means is not equal to 0
            // # 95 percent confidence interval:
            // #  -0.5911536  0.6073991
            // # sample estimates:
            // # mean of x mean of y 
            // #  10.06859  10.06046

            double[] x =
            {
                11.3709584471467, 9.43530182860391, 10.3631284113373, 10.632862604961,
                10.404268323141, 9.89387548390852, 11.5115219974389, 9.9053409615869,
                12.018423713877, 9.93728590094758, 11.3048696542235, 12.2866453927011,
                8.61113929888766, 9.72121123318263, 9.86667866360634, 10.6359503980701,
                9.71574707858393, 7.34354457909522, 7.55953307142448, 11.3201133457302,
                9.69336140592153, 8.21869156602, 9.82808264424038, 11.2146746991726,
                11.895193461265, 9.5695308683938, 9.74273061723107, 8.23683691480522,
                10.4600973548313, 9.36000512403988
            };
            double[] y =
            {
                11.4709584471467, 9.53530182860391, 10.4631284113373, 10.732862604961,
                10.504268323141, 9.99387548390852, 11.6115219974389, 10.0053409615869,
                12.118423713877, 10.0372859009476, 11.4048696542235, 12.3866453927011,
                8.71113929888766, 9.82121123318263, 9.96667866360634, 10.7359503980701,
                9.81574707858393, 7.44354457909522, 7.65953307142448, 11.4201133457302,
                9.79336140592152, 8.31869156602, 9.92808264424038, 11.3146746991726,
                11.995193461265, 9.6695308683938, 9.84273061723107, 8.33683691480522,
                10.5600973548313, 9.46000512403988, 10.5554501232412, 10.8048373372288,
                11.1351035219699, 9.49107362459279, 10.604955123298, 8.38299132092666,
                9.3155409916205, 9.24909240582348, 7.68579235005337, 10.1361226068923
            };
            var welch = WelchTTest.Calc(new Statistics(x), new Statistics(y));
            Assert.Equal(0.027097, welch.T, 6);
            Assert.Equal(61.716, welch.Df, 3);
            Assert.Equal(0.9785, welch.PValue, 4);
        }
    }
}