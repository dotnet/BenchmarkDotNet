using BenchmarkDotNet.Mathematics;
using Xunit;

namespace BenchmarkDotNet.Tests.Mathematics;

public class RatioStatisticsTests
{
    [Fact]
    public void SelfDivisionTest()
    {
        var stat = new Statistics(100, 200, 300);

        var divided = new RatioStatistics(stat, stat);

        Assert.Equal(1, divided.Mean);
        Assert.Equal(0, divided.StandardDeviation);
    }
}
