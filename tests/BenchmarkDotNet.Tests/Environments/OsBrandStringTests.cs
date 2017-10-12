using BenchmarkDotNet.Environments;
using Xunit;

namespace BenchmarkDotNet.Tests.Environments
{
    public class OsBrandStringTests
    {
        [Theory]
        [InlineData("6.3.9600", "Windows 8.1 (6.3.9600)")]
        [InlineData("10.0.14393", "Windows 10 Redstone 1 [1607, Anniversary Update] (10.0.14393)")]        
        public void WindowsIsPrettified(string originalVersion, string prettifiedName) =>
            Assert.Equal(prettifiedName, OsBrandStringHelper.Prettify("Windows", originalVersion));
        
        [Theory]
        [InlineData("10.0.15063", 674, "Windows 10 Redstone 2 [1703, Creators Update] (10.0.15063.674)")]
        public void WindowsWithUbrIsPrettified(string originalVersion, int ubr, string prettifiedName) =>
            Assert.Equal(prettifiedName, OsBrandStringHelper.Prettify("Windows", originalVersion, ubr));
    }
}