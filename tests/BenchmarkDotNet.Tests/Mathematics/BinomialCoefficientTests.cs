using BenchmarkDotNet.Mathematics;
using Xunit;

namespace BenchmarkDotNet.Tests.Mathematics
{
    public class BinomialCoefficientTests
    {
        [Theory]
        [InlineData(0, 0, 1)]
        [InlineData(1, 0, 1)]
        [InlineData(1, 1, 1)]
        [InlineData(2, 0, 1)]
        [InlineData(2, 1, 2)]
        [InlineData(2, 2, 1)]
        [InlineData(5, 0, 1)]
        [InlineData(5, 1, 5)]
        [InlineData(5, 2, 10)]
        [InlineData(5, 3, 10)]
        [InlineData(5, 4, 5)]
        [InlineData(5, 5, 1)]
        [InlineData(20, 10, 184756)]
        [InlineData(30, 15, 155117520)]
        [InlineData(40, 20, 137846528820)]
        [InlineData(64, 32, 1832624140942590534)]
        [InlineData(65, 32, 3609714217008132870)]
        public void BinomialCoefficientTest(int n, int k, long expected)
        {
            long actual = BinomialCoefficientHelper.GetBinomialCoefficient(n, k);
            Assert.Equal(expected, actual);
        }
    }
}