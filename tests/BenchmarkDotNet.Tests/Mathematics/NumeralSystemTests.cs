using BenchmarkDotNet.Mathematics;
using Xunit;

namespace BenchmarkDotNet.Tests.Mathematics
{
    public class NumeralSystemTests
    {
        [Fact]
        public void ArabicTest()
        {
            Check(NumeralSystem.Arabic, new[] { "1", "2", "3", "4", "5" });
        }

        [Fact]
        public void StarsTest()
        {
            Check(NumeralSystem.Stars, new[] { "*", "**", "***", "****", "*****" });
        }

        [Fact]
        public void RomanTest()
        {
            Check(NumeralSystem.Roman, new[] { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" });
        }

        private static void Check(NumeralSystem system, string[] expected)
        {
            for (int i = 1; i <= expected.Length; i++)
                Assert.Equal(expected[i - 1], system.ToPresentation(i));
        }
    }
}