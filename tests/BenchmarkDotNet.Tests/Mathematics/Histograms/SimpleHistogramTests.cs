using BenchmarkDotNet.Mathematics.Histograms;
using Xunit;
using Xunit.Abstractions;

namespace BenchmarkDotNet.Tests.Mathematics.Histograms
{
    public class SimpleHistogramTests
    {
        private readonly ITestOutputHelper output;

        public SimpleHistogramTests(ITestOutputHelper output) => this.output = output;

        [Fact]
        public void TrivialTest1()
        {
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Simple,
                new[] { 1.0, 2.0, 3.0, 4.0, 5.0 },
                1,
                new[]
                {
                    new[] { 1.0 },
                    new[] { 2.0 },
                    new[] { 3.0 },
                    new[] { 4.0 },
                    new[] { 5.0 }
                });
        }

        [Fact]
        public void TrivialTest2()
        {
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Simple,
                new[] { 1.0, 2.0, 3.0, 4.0, 5.0 },
                2.5,
                new[]
                {
                    new[] { 1.0, 2.0 },
                    new[] { 3.0, 4.0 },
                    new[] { 5.0 }
                });
        }

        [Fact]
        public void TrivialTest3()
        {
            HistogramTestHelper.DoHistogramTest(output, HistogramBuilder.Simple,
                new[] { 1.0, 1.1, 1.2, 1.3, 1.4, 1.5, 2.7 }, 2.0,
                new[]
                {
                    new[] { 1.0, 1.1, 1.2, 1.3, 1.4, 1.5 },
                    new[] { 2.7 }
                });
        }
    }
}