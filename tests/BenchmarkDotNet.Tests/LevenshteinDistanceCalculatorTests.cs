using BenchmarkDotNet.ConsoleArguments;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class LevenshteinDistanceCalculatorTests
    {
        private readonly LevenshteinDistanceCalculator calculator;

        public LevenshteinDistanceCalculatorTests()
        {
            calculator = new LevenshteinDistanceCalculator();
        }

        [Fact]
        public void EmptyEmpty()
        {
            Assert.Equal(0, calculator.Calculate("", ""));
        }

        [Fact]
        public void SelfSelf()
        {
            Assert.Equal(0, calculator.Calculate("string", "string"));
        }

        [Fact]
        public void TheOnlyDifference()
        {
            Assert.Equal(1, calculator.Calculate("strng", "string"));
        }

        [Fact]
        public void AllDifferences()
        {
            Assert.Equal(5, calculator.Calculate("abcde", "fghij"));
        }

        [Fact]
        public void EmptyString()
        {
            Assert.Equal(5, calculator.Calculate("", "fghij"));
        }

        [Fact]
        public void Symmetric()
        {
            string first = "first";
            string second = "second";
            Assert.Equal(calculator.Calculate(first, second), calculator.Calculate(second, first));
        }
    }
}