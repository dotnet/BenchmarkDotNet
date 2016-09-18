using BenchmarkDotNet.Mathematics;
using Xunit;

namespace BenchmarkDotNet.Tests.Mathematics
{
    public class RankTests
    {
        [Fact]
        public void RankTest()
        {
            var s1 = new Statistics(100, 101, 100, 101);
            var s2 = new Statistics(300, 301, 300, 301);
            var s3 = new Statistics(200.3279, 200.3178, 200.4046);
            var s4 = new Statistics(200.2298, 200.5738, 200.3582);
            var s5 = new Statistics(195, 196, 195, 196);
            var actualRanks = RankHelper.GetRanks(s1, s2, s3, s4, s5);
            var expectedRanks = new[] { 1, 4, 3, 3, 2 };
            Assert.Equal(expectedRanks, actualRanks);
        }
    }
}