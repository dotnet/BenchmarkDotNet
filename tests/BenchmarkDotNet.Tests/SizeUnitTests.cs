using BenchmarkDotNet.Extensions;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class SizeUnitTests
    {
        [Theory]
        [InlineData("0 B", 0)]
        [InlineData("1 B", 1)]
        [InlineData("10 B", 10)]
        [InlineData("100 B", 100)]
        [InlineData("1000 B", 1000)]
        [InlineData("1023 B", 1023)]
        [InlineData("1 KB", 1024)]
        [InlineData("1 KB", 1025)]
        [InlineData("1.07 KB", 1100)]
        [InlineData("1.5 KB", 1024 + 512)]
        [InlineData("10 KB", 10 * 1024)]
        [InlineData("1023 KB", 1023 * 1024)]
        [InlineData("1 MB", 1024 * 1024)]
        [InlineData("1 GB", 1024 * 1024 * 1024)]
        [InlineData("1 TB", 1024L * 1024 * 1024 * 1024)]
        public void SizeUnitFormattingTest(string expected, long bytes)
        {
            Assert.Equal(expected, bytes.ToSizeStr());
        }
    }
}