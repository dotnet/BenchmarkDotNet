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
        [InlineData("10.0.10240", 17797, "Windows 10 Threshold 1 [1507, RTM] (10.0.10240.17797)")]
        [InlineData("10.0.10586", 1478, "Windows 10 Threshold 2 [1511, November Update] (10.0.10586.1478)")]
        [InlineData("10.0.14393", 2156, "Windows 10 Redstone 1 [1607, Anniversary Update] (10.0.14393.2156)")]
        [InlineData("10.0.15063", 997, "Windows 10 Redstone 2 [1703, Creators Update] (10.0.15063.997)")]
        [InlineData("10.0.16299", 334, "Windows 10 Redstone 3 [1709, Fall Creators Update] (10.0.16299.334)")]
        public void WindowsWithUbrIsPrettified(string originalVersion, int ubr, string prettifiedName)
        {
            string prettified = OsBrandStringHelper.Prettify("Windows", originalVersion, ubr);
            Assert.Equal(prettifiedName, prettified);
        }
    }
}