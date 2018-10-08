using BenchmarkDotNet.Mathematics;
using Xunit;

namespace BenchmarkDotNet.Tests.Mathematics
{
    public class MathHelperTests
    {
        [Theory]
        [InlineData(-1.8084, 507.2, 0.03556814)]
        [InlineData(1.8084, 507.2, 0.9644319)]
        [InlineData(-1.488, 507.2, 0.06868611)]
        [InlineData(1.488, 507.2, 0.9313139)]
        [InlineData(-1.488, 20, 0.07617457)]
        [InlineData(1.488, 20, 0.9238254)]
        public void StudentOneTailTest(double t, double n, double expected)
        {
            double actual = MathHelper.StudentOneTail(t, n);
            Assert.Equal(expected, actual, 4);
        }

        [Theory]
        [InlineData(ConfidenceLevel.L95, 2, 12.706205)]
        [InlineData(ConfidenceLevel.L95, 3, 4.302653)]
        [InlineData(ConfidenceLevel.L95, 4, 3.182446)]
        [InlineData(ConfidenceLevel.L95, 5, 2.776445)]
        [InlineData(ConfidenceLevel.L95, 6, 2.570582)]
        [InlineData(ConfidenceLevel.L95, 7, 2.446912)]
        [InlineData(ConfidenceLevel.L95, 8, 2.364624)]
        [InlineData(ConfidenceLevel.L95, 9, 2.306004)]
        [InlineData(ConfidenceLevel.L95, 10, 2.262157)]
        [InlineData(ConfidenceLevel.L95, 11, 2.228139)]
        [InlineData(ConfidenceLevel.L95, 12, 2.200985)]
        [InlineData(ConfidenceLevel.L95, 13, 2.178813)]
        [InlineData(ConfidenceLevel.L95, 14, 2.160369)]
        [InlineData(ConfidenceLevel.L95, 15, 2.144787)]
        [InlineData(ConfidenceLevel.L95, 16, 2.131450)]
        [InlineData(ConfidenceLevel.L95, 17, 2.119905)]
        [InlineData(ConfidenceLevel.L95, 18, 2.109816)]
        [InlineData(ConfidenceLevel.L95, 19, 2.100922)]
        [InlineData(ConfidenceLevel.L95, 20, 2.093024)]
        [InlineData(ConfidenceLevel.L95, 100, 1.984217)]
        [InlineData(ConfidenceLevel.L99, 2, 63.656741)]
        [InlineData(ConfidenceLevel.L99, 3, 9.924843)]
        [InlineData(ConfidenceLevel.L99, 4, 5.840909)]
        [InlineData(ConfidenceLevel.L99, 5, 4.604095)]
        [InlineData(ConfidenceLevel.L99, 6, 4.032143)]
        [InlineData(ConfidenceLevel.L99, 7, 3.707428)]
        [InlineData(ConfidenceLevel.L99, 8, 3.499483)]
        [InlineData(ConfidenceLevel.L99, 9, 3.355387)]
        [InlineData(ConfidenceLevel.L99, 10, 3.249836)]
        [InlineData(ConfidenceLevel.L99, 11, 3.169273)]
        [InlineData(ConfidenceLevel.L99, 12, 3.105807)]
        [InlineData(ConfidenceLevel.L99, 13, 3.054540)]
        [InlineData(ConfidenceLevel.L99, 14, 3.012276)]
        [InlineData(ConfidenceLevel.L99, 15, 2.976843)]
        [InlineData(ConfidenceLevel.L99, 16, 2.946713)]
        [InlineData(ConfidenceLevel.L99, 17, 2.920782)]
        [InlineData(ConfidenceLevel.L99, 18, 2.898231)]
        [InlineData(ConfidenceLevel.L99, 19, 2.878440)]
        [InlineData(ConfidenceLevel.L99, 20, 2.860935)]
        [InlineData(ConfidenceLevel.L99, 100, 2.626405)]
        [InlineData(ConfidenceLevel.L999, 2, 636.619249)]
        [InlineData(ConfidenceLevel.L999, 3, 31.599055)]
        [InlineData(ConfidenceLevel.L999, 4, 12.923979)]
        [InlineData(ConfidenceLevel.L999, 5, 8.610302)]
        [InlineData(ConfidenceLevel.L999, 6, 6.868827)]
        [InlineData(ConfidenceLevel.L999, 7, 5.958816)]
        [InlineData(ConfidenceLevel.L999, 8, 5.407883)]
        [InlineData(ConfidenceLevel.L999, 9, 5.041305)]
        [InlineData(ConfidenceLevel.L999, 10, 4.780913)]
        [InlineData(ConfidenceLevel.L999, 11, 4.586894)]
        [InlineData(ConfidenceLevel.L999, 12, 4.436979)]
        [InlineData(ConfidenceLevel.L999, 13, 4.317791)]
        [InlineData(ConfidenceLevel.L999, 14, 4.220832)]
        [InlineData(ConfidenceLevel.L999, 15, 4.140454)]
        [InlineData(ConfidenceLevel.L999, 16, 4.072765)]
        [InlineData(ConfidenceLevel.L999, 17, 4.014996)]
        [InlineData(ConfidenceLevel.L999, 18, 3.965126)]
        [InlineData(ConfidenceLevel.L999, 19, 3.921646)]
        [InlineData(ConfidenceLevel.L999, 20, 3.883406)]
        [InlineData(ConfidenceLevel.L999, 100, 3.391529)]
        public void ZValueTest(ConfidenceLevel level, int n, double expected)
        {
            double actual = level.GetZValue(n);
            Assert.Equal(expected, actual, 3);
        }

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
        public void BinomialCoefficientTest(int n, int k, long expected)
        {
            var actual = MathHelper.BinomialCoefficient(n, k);
            Assert.Equal(expected, actual);
        }
    }
}