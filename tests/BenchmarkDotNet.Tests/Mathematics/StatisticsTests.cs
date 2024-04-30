using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Mathematics;
using BenchmarkDotNet.Tests.Common;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Mathematics;

public class StatisticsTests(ITestOutputHelper output)
{
    private void Print(Statistics summary)
    {
        output.WriteLine("N = " + summary.N);
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
        output.WriteLine("Outlier = [" + string.Join("; ", summary.AllOutliers) + "]");
        output.WriteLine("CI = " + summary.PerfolizerConfidenceInterval.ToString(TestCultureInfo.Instance));
        output.WriteLine("Percentiles = " + summary.Percentiles.ToString(TestCultureInfo.Instance));
    }

    [Fact]
    public void StatisticsWithN0Test()
    {
        Assert.Throws<ArgumentException>(() => new Statistics());
    }

    [Fact]
    public void StatisticsWithN1Test()
    {
        var summary = new Statistics(1);
        Print(summary);
        AssertEqual(1, summary.Min);
        AssertEqual(1, summary.LowerFence);
        AssertEqual(1, summary.Q1);
        AssertEqual(1, summary.Median);
        AssertEqual(1, summary.Mean);
        AssertEqual(1, summary.Q3);
        AssertEqual(1, summary.UpperFence);
        AssertEqual(1, summary.Max);
        AssertEqual(0, summary.InterquartileRange);
        AssertEqual(0, summary.StandardDeviation);
        AssertEqual(Array.Empty<double>(), summary.AllOutliers);
        AssertEqual(1, summary.Percentiles.P0);
        AssertEqual(1, summary.Percentiles.P25);
        AssertEqual(1, summary.Percentiles.P50);
        AssertEqual(1, summary.Percentiles.P85);
        AssertEqual(1, summary.Percentiles.P95);
        AssertEqual(1, summary.Percentiles.P100);
    }

    [Fact]
    public void StatisticsWithN2Test()
    {
        var summary = new Statistics(1, 2);
        Print(summary);
        AssertEqual(1, summary.Min);
        AssertEqual(0.5, summary.LowerFence);
        AssertEqual(1.25, summary.Q1);
        AssertEqual(1.5, summary.Median);
        AssertEqual(1.5, summary.Mean);
        AssertEqual(1.75, summary.Q3);
        AssertEqual(2.5, summary.UpperFence);
        AssertEqual(2, summary.Max);
        AssertEqual(0.5, summary.InterquartileRange);
        AssertEqual(0.70711, summary.StandardDeviation, AbsoluteEqualityComparer.E4);
        AssertEqual(Array.Empty<double>(), summary.AllOutliers);
        AssertEqual(1, summary.Percentiles.P0);
        AssertEqual(1.25, summary.Percentiles.P25);
        AssertEqual(1.5, summary.Percentiles.P50);
        AssertEqual(1.85, summary.Percentiles.P85);
        AssertEqual(1.95, summary.Percentiles.P95);
        AssertEqual(2, summary.Percentiles.P100);
    }

    [Fact]
    public void StatisticsWithN3Test()
    {
        var summary = new Statistics(1, 2, 4);
        Print(summary);
        AssertEqual(1, summary.Min);
        AssertEqual(-0.75, summary.LowerFence);
        AssertEqual(1.5, summary.Q1);
        AssertEqual(2, summary.Median);
        AssertEqual(2.333333, summary.Mean, AbsoluteEqualityComparer.E4);
        AssertEqual(3, summary.Q3);
        AssertEqual(5.25, summary.UpperFence);
        AssertEqual(4, summary.Max);
        AssertEqual(1.5, summary.InterquartileRange);
        AssertEqual(1.52753, summary.StandardDeviation, AbsoluteEqualityComparer.E4);
        AssertEqual(Array.Empty<double>(), summary.AllOutliers);
        AssertEqual(1, summary.Percentiles.P0);
        AssertEqual(1.5, summary.Percentiles.P25);
        AssertEqual(2, summary.Percentiles.P50);
        AssertEqual(3.4, summary.Percentiles.P85);
        AssertEqual(3.8, summary.Percentiles.P95);
        AssertEqual(4, summary.Percentiles.P100);
    }

    [Fact]
    public void StatisticsWithN7Test()
    {
        var summary = new Statistics(1, 2, 4, 8, 16, 32, 64);
        Print(summary);
        AssertEqual(1, summary.Min);
        AssertEqual(-28.5, summary.LowerFence);
        AssertEqual(3, summary.Q1);
        AssertEqual(8, summary.Median);
        AssertEqual(18.1428571429, summary.Mean, AbsoluteEqualityComparer.E5);
        AssertEqual(24, summary.Q3);
        AssertEqual(55.5, summary.UpperFence);
        AssertEqual(64, summary.Max);
        AssertEqual(21, summary.InterquartileRange);
        AssertEqual(22.9378, summary.StandardDeviation, AbsoluteEqualityComparer.E4);
        AssertEqual(new[] { 64.0 }, summary.AllOutliers);
        AssertEqual(1, summary.Percentiles.P0);
        AssertEqual(3, summary.Percentiles.P25);
        AssertEqual(8, summary.Percentiles.P50);
        AssertEqual(35.2, summary.Percentiles.P85, AbsoluteEqualityComparer.E4);
        AssertEqual(54.4, summary.Percentiles.P95, AbsoluteEqualityComparer.E4);
        AssertEqual(64, summary.Percentiles.P100);
    }

    [Fact]
    public void OutlierTest()
    {
        var summary = new Statistics(1, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 10, 10.1);
        Print(summary);
        AssertEqual(new[] { 10, 10.1 }, summary.AllOutliers);
        AssertEqual(new[] { 10, 10.1 }, summary.UpperOutliers);
        AssertEqual(Array.Empty<double>(), summary.LowerOutliers);
    }

    [Fact]
    public void ConfidenceIntervalTest()
    {
        var summary = new Statistics(Enumerable.Range(1, 30));
        Print(summary);
        Assert.Equal("99.9%", summary.PerfolizerConfidenceInterval.ConfidenceLevel.ToString());
        AssertEqual(15.5, summary.PerfolizerConfidenceInterval.Estimation);
        AssertEqual(9.618329, summary.PerfolizerConfidenceInterval.Lower, AbsoluteEqualityComparer.E4);
        AssertEqual(21.38167, summary.PerfolizerConfidenceInterval.Upper, AbsoluteEqualityComparer.E4);
    }

    [Fact]
    public void PercentileValuesWithN30Test()
    {
        var summary = new Statistics(Enumerable.Range(1, 30));
        Print(summary);
        AssertEqual(1, summary.Percentiles.P0);
        AssertEqual(8.25, summary.Percentiles.P25);
        AssertEqual(15.5, summary.Percentiles.P50);
        AssertEqual(20.43, summary.Percentiles.P67, AbsoluteEqualityComparer.E4);
        AssertEqual(24.2, summary.Percentiles.P80, AbsoluteEqualityComparer.E4);
        AssertEqual(25.65, summary.Percentiles.P85);
        AssertEqual(27.1, summary.Percentiles.P90);
        AssertEqual(28.55, summary.Percentiles.P95, AbsoluteEqualityComparer.E4);
        AssertEqual(30, summary.Percentiles.P100);
    }

    [Fact]
    public void PercentileValuesWithN90Test()
    {
        var a = Enumerable.Range(1, 30);
        var b = Enumerable.Repeat(0, 30).Concat(a);
        var c = b.Concat(Enumerable.Repeat(31, 30));
        var summary = new Statistics(c);
        Print(summary);
        AssertEqual(0, summary.Percentiles.P0);
        AssertEqual(0, summary.Percentiles.P25);
        AssertEqual(15.5, summary.Percentiles.P50);
        AssertEqual(30.63, summary.Percentiles.P67, AbsoluteEqualityComparer.E4);
        AssertEqual(31, summary.Percentiles.P80);
        AssertEqual(31, summary.Percentiles.P85);
        AssertEqual(31, summary.Percentiles.P90);
        AssertEqual(31, summary.Percentiles.P95);
        AssertEqual(31, summary.Percentiles.P100);
    }

    [AssertionMethod]
    private static void AssertEqual(double a, double b, IEqualityComparer<double>? comparer = null)
    {
        Assert.Equal(a, b, comparer ?? AbsoluteEqualityComparer.E9);
    }

    [AssertionMethod]
    private static void AssertEqual(double[] a, double[] b, IEqualityComparer<double>? comparer = null)
    {
        Assert.Equal(a, b, comparer ?? AbsoluteEqualityComparer.E9);
    }
}