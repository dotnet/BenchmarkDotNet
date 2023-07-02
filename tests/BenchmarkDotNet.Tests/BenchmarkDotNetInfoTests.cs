using System;
using BenchmarkDotNet.Properties;
using Xunit;

namespace BenchmarkDotNet.Tests
{
    public class BenchmarkDotNetInfoTests
    {
        [Theory]
        [InlineData("1.0.0", "1.0.0", false, false, true)]
        [InlineData("1.0.0", "1.0.0-develop", true, false, false)]
        [InlineData("1.0.0", "1.0.0-develop123", true, false, false)]
        [InlineData("1.2.3.4", "1.2.3.4", false, true, false)]
        public void BenchmarkDotNetInfoTest(string assemblyVersion, string fullVersion, bool expectedIsDevelop, bool expectedIsNightly,
            bool expectedIsRelease)
        {
            var info = new BenchmarkDotNetInfo(Version.Parse(assemblyVersion), fullVersion);
            Assert.Equal(expectedIsDevelop, info.IsDevelop);
            Assert.Equal(expectedIsNightly, info.IsNightly);
            Assert.Equal(expectedIsRelease, info.IsRelease);
        }
    }
}